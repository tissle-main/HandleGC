namespace HandleGC;

public unsafe sealed class RegionEvacuator
{
    private readonly GlobalHeap _globalHeap;
    private readonly GarbageCollector _gc;
    private readonly HeapScanner _scanner;
    private readonly HandleTable _handleTable;

    public RegionEvacuator(
        GlobalHeap globalHeap,
        GarbageCollector gc,
        HeapScanner scanner,
        HandleTable handleTable)
    {
        _globalHeap =
            globalHeap
            ?? throw new ArgumentNullException(nameof(globalHeap));

        _gc =
            gc
            ?? throw new ArgumentNullException(nameof(gc));

        _scanner =
            scanner
            ?? throw new ArgumentNullException(nameof(scanner));

        _handleTable =
            handleTable
            ?? throw new ArgumentNullException(nameof(handleTable));
    }

    public bool Evacuate(
        AllocationRegion region)
    {
        if(region.State != AllocationRegionState.Retired &&
            region.State != AllocationRegionState.Compacting)
        {
            throw new InvalidOperationException(
                $"Cannot evacuate region in state {region.State}.");
        }

        if(region.State == AllocationRegionState.Retired)
        {
            region.BeginCompaction();
        }

        bool completed = true;

        _scanner.Scan(
            region,
            (sourceAddress, header) =>
            {
                if(!header->Alive)
                    return;

                uint handleIndex =
                    header->HandleIndex;

                Handle handle =
                    _handleTable.Read(handleIndex);

                // Object has already been evacuated
                // during a previous attempt.
                if(handle.Address != sourceAddress)
                    return;

                uint length =
                    header->Length;

                nint destination =
                    _globalHeap.Allocate(length);

                if(destination == 0)
                {
                    completed = false;
                    return;
                }

                bool moved =
                    _gc.MoveObject(
                        sourceAddress,
                        destination);

                if(!moved)
                {
                    completed = false;
                }
            });

        if(!completed)
        {
            region.AbortCompaction();
            return false;
        }

        region.CompleteCompaction(
            region.Region.Start);

        return true;
    }
}