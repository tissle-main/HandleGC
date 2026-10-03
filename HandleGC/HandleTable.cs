using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace HandleGC;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public unsafe sealed class HandleTable
{
    public const nuint EntrySize = 8;

    private readonly VirtualRegion _region;
    private readonly uint _capacity;

    private int _nextIndex;

    public VirtualRegion Region =>
        _region;

    public uint Capacity =>
        _capacity;

    public uint UsedCount =>
        (uint)Math.Max(
            0,
            Volatile.Read(ref _nextIndex));

    public HandleTable(
        UpperAddressSpace addressSpace,
        VirtualRegion region,
        uint capacity)
    {
        if(addressSpace is null)
            throw new ArgumentNullException(
                nameof(addressSpace));

        if(capacity == 0)
            throw new ArgumentOutOfRangeException(
                nameof(capacity));

        nuint requiredSize =
            checked((nuint)capacity * EntrySize);

        if(region.Size < requiredSize)
        {
            throw new ArgumentException(
                "Region is too small for HandleTable.",
                nameof(region));
        }

        _region = region;
        _capacity = capacity;

        addressSpace.Commit(
            region.Start,
            requiredSize);
    }

    public uint Allocate(Handle handle)
    {
        int index =
            Interlocked.Increment(
                ref _nextIndex) - 1;

        if((uint)index >= _capacity)
        {
            Interlocked.Decrement(
                ref _nextIndex);

            throw new InvalidOperationException(
                "HandleTable is full.");
        }

        WriteRaw(
            (uint)index,
            handle.RawValue);

        return (uint)index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle Read(uint index)
    {
        return new Handle(
            ReadRaw(index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadRaw(uint index)
    {
        ValidateIndex(index);

        long* entry =
            GetEntryPointer(index);

        return unchecked(
            (ulong)Volatile.Read(
                ref *entry));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(
        uint index,
        Handle handle)
    {
        WriteRaw(
            index,
            handle.RawValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteRaw(
        uint index,
        ulong value)
    {
        ValidateIndex(index);

        long* entry =
            GetEntryPointer(index);

        Volatile.Write(
            ref *entry,
            unchecked((long)value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HandleEntryRef GetEntry(
        uint index)
    {
        ValidateIndex(index);

        return new HandleEntryRef(
            GetEntryPointer(index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long* GetEntryPointer(
        uint index)
    {
        return (long*)(
            (byte*)_region.Start +
            ((nuint)index * EntrySize));
    }

    private void ValidateIndex(
        uint index)
    {
        if(index >= _capacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }
    }
}