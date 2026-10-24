using System.Runtime.CompilerServices;

namespace HandleGC.UAS.Handles;

internal static unsafe class Handle
{
    private const ulong AddressMask    = 0b00000000_00000000_01111111_11111111_11111111_11111111_11111111_11111111;
    private const ulong UsageCountMask = 0b00111111_11111111_10000000_00000000_00000000_00000000_00000000_00000000;
    private const ulong GCLockedMask   = 0b01000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000;
    private const ulong RSFlagMask     = 0b10000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000;
    private const int AddressLength = 47;
    private const int UsageCountShift = AddressLength;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte* GetObjectAddress(ulong handle)
    {
        return (byte*)(handle & AddressMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong SetObjectAddress(ulong handle, byte* address)
    {
        return (handle & ~AddressMask) | ((ulong)address & AddressMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong GetUsageCount(ulong handle)
    {
        return (handle & UsageCountMask) >> UsageCountShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong SetUsageCount(ulong handle, ulong value)
    {
        return (handle & ~UsageCountMask) | ((value << UsageCountShift) & UsageCountMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetGCLocked(ulong handle)
    {
        return (handle & GCLockedMask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong SetGCLocked(ulong handle, bool value)
    {
        return value ? (handle | GCLockedMask) : (handle & ~GCLockedMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetRSFlag(ulong handle)
    {
        return (handle & RSFlagMask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong SetRSFlag(ulong handle, bool value)
    {
        return value ? (handle | RSFlagMask) : (handle & ~RSFlagMask);
    }
}