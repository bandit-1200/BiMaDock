using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BiMaDock.UITests;

/// <summary>
/// Kleines WPF-Fenster im Testprozess, das beim Ziehen Daten anbietet – standardmäßig eine Datei
/// als FileDrop wie der Windows-Explorer, alternativ beliebige Daten (z. B. Text oder Links).
/// Läuft auf einem eigenen STA-Thread.
/// </summary>
public sealed class FileDragSource : IDisposable
{
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private Window? window;

    public System.Drawing.Point Center { get; private set; }

    public FileDragSource(string filePath, int left, int top)
        : this(() => new DataObject(DataFormats.FileDrop, new[] { filePath }), left, top)
    {
    }

    public FileDragSource(Func<DataObject> createData, int left, int top)
    {
        thread = new Thread(() =>
        {
            var border = new Border
            {
                Background = Brushes.SteelBlue,
                Child = new TextBlock { Text = "Drag-Quelle", Foreground = Brushes.White, Margin = new Thickness(8) }
            };
            border.MouseLeftButtonDown += (_, _) =>
                DragDrop.DoDragDrop(border, createData(), DragDropEffects.Copy);

            window = new Window
            {
                Title = "BiMaDock UI-Test Drag-Quelle",
                Left = left,
                Top = top,
                Width = 160,
                Height = 120,
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true,
                ShowActivated = true,
                Content = border
            };
            window.ContentRendered += (_, _) =>
            {
                var source = PresentationSource.FromVisual(border);
                var topLeft = border.PointToScreen(new Point(0, 0));
                Center = new System.Drawing.Point(
                    (int)(topLeft.X + border.ActualWidth * (source?.CompositionTarget.TransformToDevice.M11 ?? 1) / 2),
                    (int)(topLeft.Y + border.ActualHeight * (source?.CompositionTarget.TransformToDevice.M22 ?? 1) / 2));
                ready.Set();
            };
            window.Show();
            System.Windows.Threading.Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        if (!ready.Wait(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException("Drag-Quellfenster wurde nicht angezeigt.");
        }
    }

    public void Dispose()
    {
        window?.Dispatcher.Invoke(() =>
        {
            window.Close();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
        });
        thread.Join(TimeSpan.FromSeconds(5));
        ready.Dispose();
    }
}
