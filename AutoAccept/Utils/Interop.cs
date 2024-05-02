using System.Runtime.InteropServices;
using System.Text;

namespace AutoAccept.Utils;

internal static class Interop
{
    [Flags]
    public enum ProcessAccessFlags : uint
    {
        QueryLimitedInformation = 0x1000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(ProcessAccessFlags desiredAccess, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder exeName, ref uint size);
}
