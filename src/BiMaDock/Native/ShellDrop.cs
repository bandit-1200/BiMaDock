using System.IO;
using System.Runtime.InteropServices;
using BiMaDock.Core.Model;

namespace BiMaDock.Native;

/// <summary>Ein aus der Shell gezogenes Element (z. B. aus dem Startmenü): Ziel zum Starten und Anzeigename.</summary>
internal sealed record ShellDropItem(string Target, string Name);

/// <summary>
/// Liest das Zwischenablageformat "Shell IDList Array" (Ziehen aus dem Startmenü, von Shell-Ordnern) und löst die
/// Elemente über IShellItem auf.
/// </summary>
internal static class ShellDrop
{
    public const string Format = "Shell IDList Array";

    private const uint SIGDN_NORMALDISPLAY = 0x00000000;
    private const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;
    private const uint SIGDN_FILESYSPATH = 0x80058000;

    public static List<ShellDropItem> Read(byte[] data)
    {
        var result = new List<ShellDropItem>();
        if (ShellIdListArray.ParseOffsets(data) is not { } offsets)
        {
            return result;
        }

        IntPtr buffer = Marshal.AllocHGlobal(data.Length);
        try
        {
            Marshal.Copy(data, 0, buffer, data.Length);
            IntPtr parent = buffer + offsets.Parent;
            foreach (int childOffset in offsets.Children)
            {
                IntPtr full = ILCombine(parent, buffer + childOffset);
                if (full == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    if (Resolve(full) is { } item)
                    {
                        result.Add(item);
                    }
                }
                finally
                {
                    ILFree(full);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return result;
    }

    private static ShellDropItem? Resolve(IntPtr pidl)
    {
        object? shellItem = null;
        try
        {
            if (SHCreateItemFromIDList(pidl, typeof(IShellItem).GUID, out shellItem) != 0 || shellItem is not IShellItem item)
            {
                return null;
            }

            string? fileSystemPath = DisplayName(item, SIGDN_FILESYSPATH);
            string? parsingName = DisplayName(item, SIGDN_DESKTOPABSOLUTEPARSING);
            string? target = ShellTargets.Classify(fileSystemPath, parsingName, KnownFolderPath, File.Exists);
            if (target is null)
            {
                return null;
            }

            string name = DisplayName(item, SIGDN_NORMALDISPLAY) is { Length: > 0 } display ? display : ShellTargets.FallbackName(target);
            return new ShellDropItem(target, name);
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            if (shellItem is not null)
            {
                Marshal.ReleaseComObject(shellItem);
            }
        }
    }

    private static string? DisplayName(IShellItem item, uint kind)
    {
        if (item.GetDisplayName(kind, out IntPtr text) != 0 || text == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni(text);
        }
        finally
        {
            Marshal.FreeCoTaskMem(text);
        }
    }

    private static string? KnownFolderPath(Guid id)
    {
        if (SHGetKnownFolderPath(id, 0, IntPtr.Zero, out IntPtr text) != 0 || text == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni(text);
        }
        finally
        {
            Marshal.FreeCoTaskMem(text);
        }
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

        void GetParent(out IShellItem ppsi);

        [PreserveSig]
        int GetDisplayName(uint sigdnName, out IntPtr ppszName);
    }

    [DllImport("shell32.dll")]
    private static extern int SHCreateItemFromIDList(
        IntPtr pidl,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out object? ppv);

    [DllImport("shell32.dll")]
    private static extern IntPtr ILCombine(IntPtr pidl1, IntPtr pidl2);

    [DllImport("shell32.dll")]
    private static extern void ILFree(IntPtr pidl);

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
}
