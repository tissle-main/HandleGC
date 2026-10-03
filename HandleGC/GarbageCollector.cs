using System.Runtime.CompilerServices;

namespace HandleGC;

public unsafe sealed class GarbageCollector
{
    private readonly HandleTable _handleTable;

    public GarbageCollector(HandleTable handleTable)
    {
        _handleTable =
            handleTable ?? throw new ArgumentNullException(nameof(handleTable));
    }

    /// <summary>
    /// Переміщує один об'єкт з sourceAddress у destinationAddress.
    /// </summary>
    public bool MoveObject(
        nint sourceAddress,
        nint destinationAddress)
    {
        if(sourceAddress == 0)
            throw new ArgumentException(
                "Source address cannot be zero.",
                nameof(sourceAddress));

        if(destinationAddress == 0)
            throw new ArgumentException(
                "Destination address cannot be zero.",
                nameof(destinationAddress));

        if(sourceAddress == destinationAddress)
            return false;

        var sourceHeader =
            (ObjectHeader*)sourceAddress;

        uint handleIndex =
            sourceHeader->HandleIndex;

        uint objectLength =
            sourceHeader->Length;

        if(objectLength < ObjectHeader.Size)
            throw new InvalidOperationException(
                "Object has invalid length.");

        HandleEntryRef entry =
            _handleTable.GetEntry(handleIndex);

        // ----------------------------------------------------
        // 1. Tell mutators that this object requires
        //    synchronized access.
        // ----------------------------------------------------

        entry.SetRequiresSync();

        // ----------------------------------------------------
        // 2. Try to acquire the GC lock.
        //
        // If the object is currently being used, we do not
        // move it.
        // ----------------------------------------------------

        if(!entry.TryGCLock())
        {
            entry.ClearRequiresSync();
            return false;
        }

        try
        {
            Handle handle =
                entry.Read();

            // GC lock must guarantee that no active users
            // remain before the object is moved.
            if(handle.UsageCount != 0)
                return false;

            // ------------------------------------------------
            // 3. Copy the complete object.
            // ------------------------------------------------

            Buffer.MemoryCopy(
                (void*)sourceAddress,
                (void*)destinationAddress,
                objectLength,
                objectLength);

            // ------------------------------------------------
            // 4. Update the stable handle.
            //
            // The handle table is now exclusively owned by GC.
            // ------------------------------------------------

            entry.UpdateAddressExclusive(
                destinationAddress);

            return true;
        }
        finally
        {
            // ------------------------------------------------
            // 5. Release GC lock.
            // ------------------------------------------------

            entry.GCUnlock();

            // ------------------------------------------------
            // 6. Object no longer requires synchronized access.
            // ------------------------------------------------

            entry.ClearRequiresSync();
        }
    }
}