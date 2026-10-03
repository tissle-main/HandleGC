using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HandleGC;

public sealed class HandleEntry
{
    private long _value;

    public HandleEntry(Handle handle)
    {
        _value = unchecked((long)handle.RawValue);
    }

    // ------------------------------------------------------------
    // Read
    // ------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle Read()
    {
        return new Handle(ReadRaw());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadRaw()
    {
        return unchecked(
            (ulong)Volatile.Read(ref _value));
    }

    // ------------------------------------------------------------
    // Atomic primitive
    // ------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CompareExchange(
        ulong expected,
        ulong desired)
    {
        long previous = Interlocked.CompareExchange(
            ref _value,
            unchecked((long)desired),
            unchecked((long)expected));

        return unchecked((ulong)previous) == expected;
    }

    // ============================================================
    // MUTATOR
    // ============================================================

    /// <summary>
    /// Attempts to acquire synchronized access to the object.
    ///
    /// Success:
    ///     UsageCount++
    ///
    /// Failure:
    ///     GC currently owns the handle (GCClocked == true).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAcquire()
    {
        while(true)
        {
            ulong current = ReadRaw();

            // GC has exclusive ownership.
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
                current + (1UL << Handle.UsageCountShift);

            if(CompareExchange(current, desired))
                return true;

            // Another thread changed the Handle.
            // Retry using the new state.
        }
    }

    /// <summary>
    /// Releases previously acquired synchronized access.
    ///
    /// UsageCount--
    /// </summary>
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
                    "Release called without an active Acquire.");
            }

            ulong desired =
                current - (1UL << Handle.UsageCountShift);

            if(CompareExchange(current, desired))
                return;
        }
    }

    // ============================================================
    // GC
    // ============================================================

    /// <summary>
    /// Prevents future accesses from using the fast path.
    ///
    /// R: 0 -> 1
    /// </summary>
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

    /// <summary>
    /// Restores normal fast-path access.
    ///
    /// R: 1 -> 0
    /// </summary>
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

    /// <summary>
    /// Acquires exclusive GC ownership.
    ///
    /// L: 0 -> 1
    ///
    /// Existing users may still exist. They must finish
    /// before the object can be moved.
    /// </summary>
    public bool TryGCLock()
    {
        while(true)
        {
            ulong current = ReadRaw();

            if((current & Handle.GCClockedMask) != 0)
                return false;

            ulong desired =
                current | Handle.GCClockedMask;

            if(CompareExchange(current, desired))
                return true;
        }
    }

    /// <summary>
    /// Releases GC ownership.
    ///
    /// L: 1 -> 0
    /// </summary>
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

    // ============================================================
    // EXCLUSIVE GC OPERATION
    // ============================================================

    /// <summary>
    /// Updates the object address while GC exclusively owns
    /// the HandleEntry.
    ///
    /// Preconditions:
    ///     GCClocked == true
    ///     UsageCount == 0
    ///
    /// No CAS is required for the address update because
    /// GC has exclusive ownership.
    /// </summary>
    public void UpdateAddressExclusive(nint newAddress)
    {
        if(newAddress == 0)
        {
            throw new ArgumentException(
                "Object address cannot be zero.",
                nameof(newAddress));
        }

        while(true)
        {
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
                    nameof(newAddress),
                    $"Address must fit into {Handle.AddressBits} bits.");
            }

            ulong desired =
                (current & ~Handle.AddressMask) |
                address;

            // At this point GC owns the Handle.
            //
            // No mutator is allowed to modify this state.
            // A normal write is therefore sufficient.
            Volatile.Write(
                ref _value,
                unchecked((long)desired));

            return;
        }
    }
}