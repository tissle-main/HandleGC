using System;
using System.Runtime.CompilerServices;

namespace HandleGC;

public unsafe sealed class ObjectAllocator
{
    private readonly AllocationRegion _region;
    private readonly HandleTable _handleTable;

    public AllocationRegion Region => _region;

    public HandleTable HandleTable => _handleTable;

    public ObjectAllocator(
        AllocationRegion region,
        HandleTable handleTable)
    {
        _region = region;

        _handleTable = handleTable
            ?? throw new ArgumentNullException(
                nameof(handleTable));
    }

    public Handle Allocate(nuint dataSize)
    {
        nuint totalSize =
            checked(
                (nuint)ObjectHeader.Size +
                dataSize);

        nint objectAddress =
            _region.Allocate(totalSize);

        if(objectAddress == 0)
        {
            throw new InvalidOperationException(
                "AllocationRegion is full.");
        }

        uint handleIndex =
            _handleTable.Allocate(
                new Handle(objectAddress));

        ObjectHeader header =
            new ObjectHeader
            {
                Length = checked(
                    (uint)totalSize),

                HandleIndex = handleIndex,

                Alive = true,

                MetadataAddress = 0
            };

        *(ObjectHeader*)objectAddress =
            header;

        return new Handle(objectAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GetObjectAddress(
        Handle handle)
    {
        return handle.Address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ObjectHeader* GetHeader(
        Handle handle)
    {
        return (ObjectHeader*)handle.Address;
    }
}