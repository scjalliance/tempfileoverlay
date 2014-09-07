using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;

namespace TempFileOverlay
{
    [Flags]
    public enum HFLAGS : uint
    {
        ISIOI_ICONFILE = 0x00000001,
        ISIOI_ICONINDEX = 0x00000002
    }

    [Flags]
    public enum HRESULT : uint
    {
        S_OK = 0x00000000,
        S_FALSE = 0x00000001,
        E_FAIL = 0x80004005,
    }

    [ComVisible(false)]
    [ComImport]
    [Guid("0C6C4200-C589-11D0-999A-00C04FD655E1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellIconOverlayIdentifier
    {
        [PreserveSig]
        int IsMemberOf([MarshalAs(UnmanagedType.LPWStr)]string path, uint attributes);

        [PreserveSig]
        int GetOverlayInfo(IntPtr iconFileBuffer, int iconFileBufferSize, out int iconIndex, out uint flags);

        [PreserveSig]
        int GetPriority(out int priority);
    }

    [ComVisible(true)]
    [Guid("6e2453c9-d248-45a9-8286-00f250af8d9a")]
    public class TempFileOverlay : IShellIconOverlayIdentifier
    {
        int IShellIconOverlayIdentifier.IsMemberOf(string path, uint attributes)
        {
            try
            {
                unchecked
                {
                    return (File.GetAttributes(path) & FileAttributes.Temporary) == FileAttributes.Temporary ? (int)HRESULT.S_OK : (int)HRESULT.S_FALSE;
                }
            }
            catch
            {
                unchecked
                {
                    return (int)HRESULT.E_FAIL;
                }
            }
        }

        public int GetOverlayInfo(IntPtr iconFileBuffer, int iconFileBufferSize, out int iconIndex, out uint flags)
        {
            string iconFile = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "/TempFileOverlay/ConflictIcon.ico";
            int bytesCount = System.Text.Encoding.Unicode.GetByteCount(iconFile);
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(iconFile);

            if (bytes.Length + 2 < iconFileBufferSize)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    Marshal.WriteByte(iconFileBuffer, i, bytes[i]);
                }
                Marshal.WriteByte(iconFileBuffer, bytes.Length, 0);
                Marshal.WriteByte(iconFileBuffer, bytes.Length + 1, 0);
            }

            iconIndex = 0;
            flags = (int)(HFLAGS.ISIOI_ICONFILE | HFLAGS.ISIOI_ICONINDEX);
            return (int)HRESULT.S_OK;
        }

        int IShellIconOverlayIdentifier.GetPriority(out int priority)
        {
            priority = 0;
            return (int)HRESULT.S_OK;
        }

        public sealed class ShellInterop
        {
            private ShellInterop() { }

            [DllImport("shell32.dll")]
            public static extern void SHChangeNotify(int eventID, uint flags, IntPtr item1, IntPtr item2);
        }

        [ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            RegistryKey regKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers\ " + t.Name);
            regKey.SetValue(string.Empty, t.GUID.ToString("B").ToUpper());
            regKey.Close();
            ShellInterop.SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers\ " + t.Name);
            ShellInterop.SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
