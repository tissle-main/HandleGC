namespace HandleGC;

using System;
using System.Runtime.CompilerServices;

public readonly struct Handle
{
    /*
     * 64-bit Handle layout
     *
     * 63                                      47 46                       0
     * ┌────────────────────────────────────────┬──────────────────────────┐
     * │             CONTROL (17)               │      ADDRESS (47)       │
     * └────────────────────────────────────────┴──────────────────────────┘
     *
     * Control:
     *
     * 63                              49 48       47 46
     * ┌────────────────────────────────┬──────────┬───┐
     * │          UsageCount (15)       │ GC Lock  │ RS│
     * └────────────────────────────────┴──────────┴───┘
     */

    public const int AddressBits = 47;
    public const int ControlBits = 17;

    internal const int RequiresSyncBit = 47;
    internal const int GCClockedBit = 48;
    internal const int UsageCountShift = 49;

    internal const int UsageCountBits = 15;

    internal const ulong AddressMask =
        (1UL << AddressBits) - 1;

    internal const ulong RequiresSyncMask =
        1UL << RequiresSyncBit;

    internal const ulong GCClockedMask =
        1UL << GCClockedBit;

    internal const ulong UsageCountMask =
        (1UL << UsageCountBits) - 1;

    private readonly ulong _value;

    public Handle(
        nint address,
        ushort usageCount = 0,
        bool requiresSync = false,
        bool gclocked = false)
    {
        if(address == 0)
            throw new ArgumentException(
                "Handle address cannot be zero.",
                nameof(address));

        ulong addressValue = (ulong)address;

        if((addressValue & ~AddressMask) != 0)
            throw new ArgumentOutOfRangeException(nameof(address));

        if(usageCount > UsageCountMask)
            throw new ArgumentOutOfRangeException(nameof(usageCount));

        ulong value = addressValue;

        if(requiresSync)
            value |= RequiresSyncMask;

        if(gclocked)
            value |= GCClockedMask;

        value |=
            ((ulong)usageCount & UsageCountMask)
            << UsageCountShift;

        _value = value;
    }

    internal Handle(ulong rawValue)
    {
        _value = rawValue;
    }

    public ulong RawValue => _value;

    public nint Address
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)(_value & AddressMask);
    }

    public bool RequiresSync
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_value & RequiresSyncMask) != 0;
    }

    public bool GCLocked
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_value & GCClockedMask) != 0;
    }

    public ushort UsageCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)(
            (_value >> UsageCountShift) &
            UsageCountMask);
    }

    public override string ToString()
    {
        return
            $"Address=0x{Address:X}, " +
            $"RequiresSync={RequiresSync}, " +
            $"GCLocked={GCLocked}, " +
            $"UsageCount={UsageCount}";
    }
}