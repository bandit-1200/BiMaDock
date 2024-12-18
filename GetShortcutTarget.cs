using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace BiMaDock
{
    [Flags]
    enum SLGP_FLAGS
    {
        SLGP_SHORTPATH = 0x1,
        SLGP_UNCPRIORITY = 0x2,
        SLGP_RAWPATH = 0x4
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public long ftCreationTime;
        public long ftLastAccessTime;
        public long ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [Flags]
    enum SLR_FLAGS
    {
        SLR_NO_UI = 0x1,
        SLR_ANY_MATCH = 0x2,
        SLR_UPDATE = 0x4,
        SLR_NOUPDATE = 0x8,
        SLR_NOSEARCH = 0x10,
        SLR_NOTRACK = 0x20,
        SLR_NOLINKINFO = 0x40,
        SLR_INVOKE_MSI = 0x80
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellLinkW
    {
        void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010c-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPersist
    {
        [PreserveSig]
        void GetClassID(out Guid pClassID);
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPersistFile : IPersist
    {
        new void GetClassID(out Guid pClassID);
        [PreserveSig]
        int IsDirty();
        [PreserveSig]
        void Load([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        [PreserveSig]
        void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);
        [PreserveSig]
        void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        [PreserveSig]
        void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    public class ShellLink
    {
    }

    public class GetShortcutTarget
    {
        const uint STGM_READ = 0;
        const int MAX_PATH = 260;

        public static async Task<string> GetShortcutTargetAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string extension = System.IO.Path.GetExtension(filePath).ToLower();
                    if (extension == ".lnk")
                    {
                        ShellLink link = new ShellLink();
                        ((IPersistFile)link).Load(filePath, STGM_READ);

                        StringBuilder stringBuilder = new StringBuilder(MAX_PATH);
                        WIN32_FIND_DATAW data;

                        ((IShellLinkW)link).GetPath(stringBuilder, stringBuilder.Capacity, out data, 0);
                        return stringBuilder.ToString();
                    }
                    else if (extension == ".url")
                    {
                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                                {
                                    return line.Substring(4);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Auslesen der Verknüpfung: {ex.Message}");
                }
                return string.Empty;
            });
        }




    }
}
