// using System;
// using System.Runtime.InteropServices;
// using System.Text;

// namespace BiMaDock
// {
//     public class TestPath
//     {
//         public static string GetShortcutTarget(string filePath)
//         {
//             StringBuilder targetPath = new StringBuilder(260);
//             IShellLinkW shellLink = (IShellLinkW)new CShellLink();
//             ((IPersistFile)shellLink).Load(filePath, 0);
//             shellLink.GetPath(targetPath, targetPath.Capacity, IntPtr.Zero, 0);
//             return targetPath.ToString();
//         }

//         [ComImport, Guid("000214F9-0000-0000-C000-000000000046")]
//         private class CShellLink { }

//         [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214EE-0000-0000-C000-000000000046")]
//         private interface IShellLinkW
//         {
//             void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, int fFlags);
//         }

//         [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010b-0000-0000-C000-000000000046")]
//         private interface IPersistFile
//         {
//             void GetClassID(out Guid pClassID);
//             void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
//         }
//     }
// }
