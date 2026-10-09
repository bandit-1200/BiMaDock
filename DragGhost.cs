using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// Halbtransparentes Abbild eines gezogenen Dock-Elements, das dem Mauszeiger
/// während der modalen DoDragDrop-Schleife folgt – auch außerhalb des Dock-Fensters.
/// Das Fenster ist für Maus und OLE-Drag-Hit-Testing unsichtbar (WS_EX_TRANSPARENT | WS_EX_LAYERED)
/// und wird nie aktiviert (WS_EX_NOACTIVATE), damit es weder Drop-Ziel wird noch den Fokus stiehlt.
/// </summary>
internal sealed class DragGhost : IDisposable
{
    private const double GhostOpacity = 0.7;
    // Abstand (DIP) zwischen Mausspitze und Oberkante des Abbilds, damit die Zeigerspitze sichtbar bleibt
    private const double CursorOffsetY = 12;

    private readonly FrameworkElement source;
    private readonly Window window;
    private readonly IntPtr hwnd;
    private readonly GiveFeedbackEventHandler giveFeedbackHandler;
    private readonly QueryContinueDragEventHandler queryContinueHandler;
    private int lastCursorX = int.MinValue;
    private int lastCursorY = int.MinValue;
    private bool disposed;

    private DragGhost(FrameworkElement source, Window window)
    {
        this.source = source;
        this.window = window;
        hwnd = new WindowInteropHelper(window).Handle;

        // GiveFeedback/QueryContinueDrag feuern während der gesamten OLE-Drag-Schleife auf der Drag-Quelle,
        // auch wenn sich der Mauszeiger über fremden Fenstern befindet. handledEventsToo, damit fremde
        // Handler die Nachführung nicht unterbinden.
        giveFeedbackHandler = (_, _) => UpdatePosition();
        queryContinueHandler = (_, _) => UpdatePosition();
        source.AddHandler(DragDrop.GiveFeedbackEvent, giveFeedbackHandler, true);
        source.AddHandler(DragDrop.QueryContinueDragEvent, queryContinueHandler, true);
    }

    /// <summary>
    /// Erstellt das Abbild von <paramref name="source"/> und zeigt es am Mauszeiger an.
    /// Muss vor DoDragDrop aufgerufen werden, solange das Element noch sichtbar ist.
    /// Wirft nie; liefert bei Fehlern null.
    /// </summary>
    public static DragGhost? TryCreate(FrameworkElement source)
    {
        Window? window = null;
        try
        {
            ImageSource? image = RenderSnapshot(source);
            if (image == null)
            {
                return null;
            }

            window = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                ShowActivated = false,
                Topmost = true,
                Focusable = false,
                IsHitTestVisible = false,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.Manual,
                // Erst außerhalb des sichtbaren Bereichs anzeigen, dann an den Mauszeiger setzen (kein Aufblitzen)
                Left = -32000,
                Top = -32000,
                Opacity = GhostOpacity,
                Content = new Image
                {
                    Source = image,
                    Width = source.ActualWidth,
                    Height = source.ActualHeight,
                    Stretch = Stretch.Fill,
                    IsHitTestVisible = false
                }
            };
            window.SourceInitialized += (_, _) => ApplyClickThroughStyles(new WindowInteropHelper(window!).Handle);

            window.Show();

            var ghost = new DragGhost(source, window);
            ghost.UpdatePosition();
            return ghost;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DragGhost konnte nicht erstellt werden: {ex.Message}");
            try
            {
                window?.Close();
            }
            catch
            {
                // Aufräumen darf nicht werfen
            }
            return null;
        }
    }

    /// <summary>
    /// Setzt das Abbild horizontal zentriert knapp unter die aktuelle Mausposition.
    /// </summary>
    public void UpdatePosition()
    {
        if (disposed || hwnd == IntPtr.Zero)
        {
            return;
        }

        try
        {
            if (!GetCursorPos(out POINT cursor))
            {
                return;
            }
            if (cursor.X == lastCursorX && cursor.Y == lastCursorY)
            {
                return;
            }
            lastCursorX = cursor.X;
            lastCursorY = cursor.Y;

            // Mauskoordinaten und SetWindowPos nutzen denselben (prozessbezogenen) Gerätekoordinatenraum,
            // daher wird direkt in Gerätepixeln positioniert – unabhängig vom DPI-Modus und Monitor.
            int width;
            int height;
            if (GetWindowRect(hwnd, out RECT rect) && rect.Right > rect.Left)
            {
                width = rect.Right - rect.Left;
                height = rect.Bottom - rect.Top;
            }
            else
            {
                Vector size = ToDevice(new Vector(source.ActualWidth, source.ActualHeight));
                width = (int)Math.Round(size.X);
                height = (int)Math.Round(size.Y);
            }

            int offsetY = (int)Math.Round(ToDevice(new Vector(0, CursorOffsetY)).Y);
            int x = cursor.X - width / 2;
            int y = cursor.Y + offsetY;

            SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }
        catch (Exception ex)
        {
            // Läuft innerhalb von OLE-Callbacks – Ausnahmen dürfen den Drag nicht abbrechen
            System.Diagnostics.Debug.WriteLine($"DragGhost-Positionierung fehlgeschlagen: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        try
        {
            source.RemoveHandler(DragDrop.GiveFeedbackEvent, giveFeedbackHandler);
            source.RemoveHandler(DragDrop.QueryContinueDragEvent, queryContinueHandler);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DragGhost-Handler konnten nicht entfernt werden: {ex.Message}");
        }

        try
        {
            window.Content = null;
            window.Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DragGhost-Fenster konnte nicht geschlossen werden: {ex.Message}");
        }
    }

    private Vector ToDevice(Vector dip)
    {
        Matrix? transform = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformToDevice;
        return transform.HasValue ? transform.Value.Transform(dip) : dip;
    }

    /// <summary>
    /// Rendert das Element mit der DPI seines Monitors in ein Bitmap.
    /// Über einen VisualBrush, damit Margin/Offset des Elements das Abbild nicht verschieben.
    /// </summary>
    private static ImageSource? RenderSnapshot(FrameworkElement source)
    {
        double width = source.ActualWidth;
        double height = source.ActualHeight;
        if (width <= 0 || height <= 0 || !source.IsVisible)
        {
            return null;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(source);
        int pixelWidth = (int)Math.Ceiling(width * dpi.DpiScaleX);
        int pixelHeight = (int)Math.Ceiling(height * dpi.DpiScaleY);
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return null;
        }

        var visual = new DrawingVisual();
        using (DrawingContext context = visual.RenderOpen())
        {
            var brush = new VisualBrush(source)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
            context.DrawRectangle(brush, null, new Rect(0, 0, width, height));
        }

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// Macht das Fenster für Maus und WindowFromPoint (OLE-Drop-Ziel-Ermittlung) durchlässig
    /// und verhindert Aktivierung sowie Anzeige in Alt+Tab.
    /// </summary>
    private static void ApplyClickThroughStyles(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }
        int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
        SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOOWNERZORDER = 0x0200;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int newLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
