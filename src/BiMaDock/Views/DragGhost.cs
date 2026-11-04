using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using BiMaDock.Native;

namespace BiMaDock.Views;

/// <summary>
/// Halbtransparentes Abbild eines gezogenen Elements, das dem Mauszeiger folgt, auch außerhalb des Docks.
/// Das Fenster ist für die Maus durchlässig, damit es das Ablegen nicht abfängt.
/// </summary>
internal sealed class DragGhost : Window
{
    private const double SizeDip = 52;
    private IntPtr _hwnd;
    private double _scale = 1;

    public DragGhost(ImageSource icon)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = null;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        IsHitTestVisible = false;
        Width = SizeDip;
        Height = SizeDip;
        Content = new Image
        {
            Source = icon,
            Opacity = 0.75,
            Width = SizeDip,
            Height = SizeDip,
        };
        RenderOptions.SetBitmapScalingMode((Image)Content, BitmapScalingMode.HighQuality);
        Left = -10000;
        Top = -10000;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowStyles.MakeClickThrough(_hwnd);
        _scale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1;
    }

    /// <summary>Zentriert das Abbild unter dem Mauszeiger (physische Bildschirmkoordinaten, unabhängig von DPI).</summary>
    public void FollowCursor()
    {
        if (_hwnd == IntPtr.Zero || !WindowStyles.TryGetCursorPos(out int x, out int y))
        {
            return;
        }

        int half = (int)(SizeDip * _scale / 2);
        WindowStyles.MoveWithoutActivating(_hwnd, x - half, y - half);
    }
}
