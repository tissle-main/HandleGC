using System.Runtime.CompilerServices;

namespace HandleGC.UAS.Handles;

internal static unsafe class HandleTable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Read(uint handleIndex)
    {
        return Volatile.Read(ref UpperAddressSpace.HandleTable[handleIndex]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CompareExchange(uint handleIndex, ulong value, ulong expected)
    {
        return Interlocked.CompareExchange(ref UpperAddressSpace.HandleTable[handleIndex], value, expected) == expected;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte* GetObjectAddress(uint handleIndex)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        return Handle.GetObjectAddress(Volatile.Read(ref *handle));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetObjectAddress(uint handleIndex, byte* objAddress)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        ulong expected;
        do
        {
            expected = Volatile.Read(ref *handle);
        }
        while(Interlocked.CompareExchange(ref *handle, Handle.SetObjectAddress(expected, objAddress), expected) != expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong GetUsageCount(uint handleIndex)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        return Handle.GetUsageCount(Volatile.Read(ref *handle));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetUsageCount(uint handleIndex, ulong value)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        Update(handle, value, Handle.SetUsageCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetGCLocked(uint handleIndex)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        return Handle.GetGCLocked(Volatile.Read(ref *handle));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetGCLocked(uint handleIndex, bool value)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        Update(handle, value, Handle.SetGCLocked);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetRSFlag(uint handleIndex)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        return Handle.GetRSFlag(Volatile.Read(ref *handle));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetRSFlag(uint handleIndex, bool value)
    {
        ulong* handle = UpperAddressSpace.HandleTable + handleIndex;
        Update(handle, value, Handle.SetRSFlag);
    }

    private delegate ulong HandleUpdater(ulong handle, bool value);
    private delegate ulong UsageCountUpdater(ulong handle, ulong value);

    private static void Update(ulong* handle, bool value, HandleUpdater update)
    {
        ulong expected;
        do { expected = Volatile.Read(ref *handle); }
        while(Interlocked.CompareExchange(ref *handle, update(expected, value), expected) != expected);
    }

    private static void Update(ulong* handle, ulong value, UsageCountUpdater update)
    {
        ulong expected;
        do { expected = Volatile.Read(ref *handle); }
        while(Interlocked.CompareExchange(ref *handle, update(expected, value), expected) != expected);
    }
}
