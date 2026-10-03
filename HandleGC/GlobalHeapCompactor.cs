namespace HandleGC;

public unsafe sealed class GlobalHeapCompactor
{
    private readonly GarbageCollector _gc;

    public GlobalHeapCompactor(
        GarbageCollector gc)
    {
        _gc = gc
            ?? throw new ArgumentNullException(nameof(gc));
    }

    public nint Compact(GlobalHeap heap)
    {
        ArgumentNullException.ThrowIfNull(heap);

        nint scan =
            heap.Region.Start;

        nint destination =
            heap.Region.Start;

        nint end =
            heap.UsedEnd;

        while(scan < end)
        {
            ObjectHeader* header =
                (ObjectHeader*)scan;

            uint length =
                header->Length;

            if(length < ObjectHeader.Size)
            {
                throw new InvalidOperationException(
                    $"Invalid object length at 0x{scan:X}: {length}");
            }

            nint next =
                checked(scan + (nint)length);

            if(next > end)
            {
                throw new InvalidOperationException(
                    $"Object at 0x{scan:X} exceeds GlobalHeap boundary.");
            }

            if(header->Alive)
            {
                if(scan != destination)
                {
                    bool moved =
                        _gc.MoveObject(
                            scan,
                            destination);

                    if(!moved)
                    {
                        throw new InvalidOperationException(
                            $"Failed to move object at 0x{scan:X}.");
                    }
                }

                destination +=
                    (nint)length;
            }

            scan = next;
        }

        heap.CompleteCompaction(destination);

        return destination;
    }
}