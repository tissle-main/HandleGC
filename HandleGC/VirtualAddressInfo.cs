using System.Runtime.InteropServices;

namespace HandleGC;

public static class VirtualAddressInfo
{
    public static SystemInfo Get()
    {
        SYSTEM_INFO nativeInfo = default;

        GetNativeSystemInfo(ref nativeInfo);

        nint minimumAddress =
            nativeInfo.lpMinimumApplicationAddress;

        nint maximumAddress =
            nativeInfo.lpMaximumApplicationAddress;

        int addressBits =
            CalculateAddressBits(maximumAddress);

        return new SystemInfo(
            Architecture: RuntimeInformation.ProcessArchitecture,
            PointerSizeBits: IntPtr.Size * 8,
            VirtualAddressBits: addressBits,
            PageSize: nativeInfo.dwPageSize,
            AllocationGranularity: nativeInfo.dwAllocationGranularity,
            MinimumApplicationAddress: minimumAddress,
            MaximumApplicationAddress: maximumAddress);
    }

    private static int CalculateAddressBits(nint maximumAddress)
    {
        ulong value = (ulong)maximumAddress;

        int bits = 0;

        while(value != 0)
        {
            bits++;
            value >>= 1;
        }

        return bits;
    }

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    private static extern void GetNativeSystemInfo(
        ref SYSTEM_INFO lpSystemInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;

        public uint dwPageSize;

        public nint lpMinimumApplicationAddress;
        public nint lpMaximumApplicationAddress;

        public nuint dwActiveProcessorMask;

        public uint dwNumberOfProcessors;
        public uint dwProcessorType;

        public uint dwAllocationGranularity;

        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }
}

public readonly record struct SystemInfo(
    Architecture Architecture,
    int PointerSizeBits,
    int VirtualAddressBits,
    uint PageSize,
    uint AllocationGranularity,
    nint MinimumApplicationAddress,
    nint MaximumApplicationAddress);