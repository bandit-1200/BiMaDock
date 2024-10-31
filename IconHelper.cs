using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

public static class IconHelper
{
    [DllImport("Shell32.dll")]
    private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_SMALLICON = 0x000000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

public static BitmapSource GetIcon(string filePath)
{
    try
    {
        SHFILEINFO shinfo = new SHFILEINFO();
        SHGetFileInfo(filePath, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);

        if (shinfo.hIcon != IntPtr.Zero)
        {
            using (var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
            {
                var bitmap = icon.ToBitmap();
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                return bitmapSource;
            }
        }
        else
        {
            Console.WriteLine($"Kein Icon gefunden für: {filePath}");
            return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fehler beim Abrufen des Icons für: {filePath}, Fehler: {ex.Message}");
        return null;
    }
}

}
