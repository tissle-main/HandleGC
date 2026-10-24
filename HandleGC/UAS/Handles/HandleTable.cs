using System.Runtime.CompilerServices;

namespace HandleGC.UAS.Handles;

internal static unsafe class HandleTable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte* GetObjectAddress(uint handleIndex)
    {
        return Handle.GetObjectAddress(UpperAddressSpace.HandleTable[handleIndex]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetObjectAddress(uint handleIndex, byte* objAddress)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        *handle = Handle.SetObjectAddress(*handle, objAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong GetUsageCount(uint handleIndex)
    {
        return Handle.GetUsageCount(UpperAddressSpace.HandleTable[handleIndex]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetUsageCount(uint handleIndex, ulong value)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        *handle = Handle.SetUsageCount(*handle, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetGCLocked(uint handleIndex)
    {
        return Handle.GetGCLocked(UpperAddressSpace.HandleTable[handleIndex]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetGCLocked(uint handleIndex, bool value)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        *handle = Handle.SetGCLocked(*handle, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetRSFlag(uint handleIndex)
    {
        return Handle.GetRSFlag(UpperAddressSpace.HandleTable[handleIndex]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetRSFlag(uint handleIndex, bool value)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        *handle = Handle.SetRSFlag(*handle, value);
    }
}