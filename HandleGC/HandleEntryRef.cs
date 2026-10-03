using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HandleGC;

public unsafe readonly struct HandleEntryRef
{
    private readonly long* _value;

    internal HandleEntryRef(long* value)
    {
        _value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadRaw()
    {
        return unchecked(
            (ulong)Volatile.Read(ref *_value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle Read()
    {
        return new Handle(ReadRaw());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CompareExchange(
        ulong expected,
        ulong desired)
    {
        long previous =
            Interlocked.CompareExchange(
                ref *_value,
                unchecked((long)desired),
                unchecked((long)expected));

        return unchecked((ulong)previous) == expected;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAcquire()
    {
        while(true)
        {
            ulong current = ReadRaw();

            if((current & Handle.GCClockedMask) != 0)
                return false;

            ulong usageCount =
                (current >> Handle.UsageCountShift) &
                Handle.UsageCountMask;

            if(usageCount == Handle.UsageCountMask)
            {
                throw new InvalidOperationException(
                    "Handle usageCount overflow.");
            }

            ulong desired =
                current +
                (1UL << Handle.UsageCountShift);

            if(CompareExchange(current, desired))
                return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release()
    {
        while(true)
        {
            ulong current = ReadRaw();

            ulong usageCount =
                (current >> Handle.UsageCountShift) &
                Handle.UsageCountMask;

            if(usageCount == 0)
            {
                throw new InvalidOperationException(
                    "Release called without Acquire.");
            }

            ulong desired =
                current -
                (1UL << Handle.UsageCountShift);

            if(CompareExchange(current, desired))
                return;
        }
    }

    public void SetRequiresSync()
    {
        while(true)
        {
            ulong current = ReadRaw();

            if((current & Handle.RequiresSyncMask) != 0)
                return;

            ulong desired =
                current | Handle.RequiresSyncMask;

            if(CompareExchange(current, desired))
                return;
        }
    }

    public void ClearRequiresSync()
    {
        while(true)
        {
            ulong current = ReadRaw();

            if((current & Handle.RequiresSyncMask) == 0)
                return;

            ulong desired =
                current & ~Handle.RequiresSyncMask;

            if(CompareExchange(current, desired))
                return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGCLock()
    {
        while(true)
        {
            ulong current = ReadRaw();

            if((current & Handle.GCClockedMask) != 0)
                return false;

            if((current & Handle.RequiresSyncMask) == 0)
            {
                throw new InvalidOperationException(
                    "RequiresSync must be set before GC lock.");
            }

            ulong usageCount =
                (current >> Handle.UsageCountShift) &
                Handle.UsageCountMask;

            // GC може отримати ексклюзивне володіння
            // тільки коли об'єкт більше ніхто не використовує.
            if(usageCount != 0)
                return false;

            ulong desired =
                current | Handle.GCClockedMask;

            if(CompareExchange(current, desired))
                return true;
        }
    }

    public void GCUnlock()
    {
        while(true)
        {
            ulong current = ReadRaw();

            if((current & Handle.GCClockedMask) == 0)
            {
                throw new InvalidOperationException(
                    "Handle is not GC locked.");
            }

            ulong desired =
                current & ~Handle.GCClockedMask;

            if(CompareExchange(current, desired))
                return;
        }
    }

    public void UpdateAddressExclusive(nint newAddress)
    {
        if(newAddress == 0)
        {
            throw new ArgumentException(
                "Object address cannot be zero.",
                nameof(newAddress));
        }

        ulong current = ReadRaw();

        if((current & Handle.GCClockedMask) == 0)
        {
            throw new InvalidOperationException(
                "Handle must be GC locked.");
        }

        ulong usageCount =
            (current >> Handle.UsageCountShift) &
            Handle.UsageCountMask;

        if(usageCount != 0)
        {
            throw new InvalidOperationException(
                "Cannot update address while object is in use.");
        }

        ulong address = (ulong)newAddress;

        if((address & ~Handle.AddressMask) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newAddress));
        }

        ulong desired =
            (current & ~Handle.AddressMask) |
            address;

        Volatile.Write(
            ref *_value,
            unchecked((long)desired));
    }
}