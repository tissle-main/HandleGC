namespace HandleGC;

public unsafe sealed class HeapCompactor
{
    private readonly GarbageCollector _gc;

    public HeapCompactor(GarbageCollector gc)
    {
        _gc = gc
            ?? throw new ArgumentNullException(nameof(gc));
    }

    public nint Compact(AllocationRegion region)
    {
        region.BeginCompaction();

        try
        {
            nint scan =
                region.Region.Start;

            nint destination =
                region.Region.Start;

            nint end =
                region.UsedEnd;

            while(scan < end)
            {
                ObjectHeader* header =
                    (ObjectHeader*)scan;

                uint length =
                    header->Length;

                if(length == 0)
                    break;

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
                        $"Object at 0x{scan:X} exceeds used region boundary.");
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

            region.CompleteCompaction(
                destination);

            return destination;
        }
        catch
        {
            // Поки що не вводимо окремий Failed state.
            // Region залишається COMPACTING, щоб не приховувати
            // потенційно пошкоджений стан.
            throw;
        }
    }
}