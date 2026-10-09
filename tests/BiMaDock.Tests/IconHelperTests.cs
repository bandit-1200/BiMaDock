using System.Windows.Media.Imaging;
using Xunit;

namespace BiMaDock.Tests;

public class IconHelperTests
{
    [Fact]
    public void GetIcon_ProgrammLiefertHochaufloesendesIconMitTransparenz()
    {
        IconHelper.ClearCache();

        BitmapSource icon = IconHelper.GetIcon(@"C:\Windows\System32\notepad.exe", string.Empty);

        Assert.True(icon.PixelWidth >= 64, $"Icon ist nur {icon.PixelWidth} px breit.");
        Assert.True(icon.IsFrozen);
        Assert.True(HasTransparentPixel(icon), "Icon hat keinen transparenten Hintergrund.");
    }

    [Fact]
    public void GetIcon_OrdnerLiefertHochaufloesendesIcon()
    {
        IconHelper.ClearCache();

        BitmapSource icon = IconHelper.GetIcon(@"C:\Windows", string.Empty);

        Assert.True(icon.PixelWidth >= 64, $"Icon ist nur {icon.PixelWidth} px breit.");
    }

    [Fact]
    public void GetIcon_FehlendeDateiLiefertPlatzhalterStattFehler()
    {
        BitmapSource icon = IconHelper.GetIcon(@"C:\BiMaDock-Test\fehlt.exe", @"C:\BiMaDock-Test\fehlt.png");

        Assert.NotNull(icon);
    }

    private static bool HasTransparentPixel(BitmapSource icon)
    {
        var converted = new FormatConvertedBitmap(icon, System.Windows.Media.PixelFormats.Bgra32, null, 0);
        var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
        converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
        for (int i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] < 255)
            {
                return true;
            }
        }
        return false;
    }
}
