using System.Runtime.InteropServices;

namespace HandleGC.Imports;

internal static unsafe partial class WinImports
{
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_READWRITE = 0x04;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint VirtualAlloc(nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    internal static byte* ReserveAndCommit(ulong size)
    {
        nint address = VirtualAlloc(0, (nuint)size, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
        if(address == 0)
        {
            throw new InvalidOperationException($"VirtualAlloc failed. Win32 error: {Marshal.GetLastPInvokeError()}");
        }
        return (byte*)address;
    }
}