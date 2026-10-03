namespace HandleGC;

public sealed class AllocationRegion
{
    private nint _allocationPointer;

    public VirtualRegion Region { get; }

    public nint AllocationPointer =>
        _allocationPointer;

    public nint UsedEnd =>
        _allocationPointer;

    public nuint UsedSize
    {
        get => checked((nuint)(_allocationPointer - Region.Start));
        set
        {
            _allocationPointer = checked(Region.Start + (nint)value);
        }
    }

    public nuint FreeSize =>
        checked((nuint)(
            Region.End - _allocationPointer));

    public bool IsFull =>
        _allocationPointer >= Region.End;

    public AllocationRegionState State { get; private set; }

    public AllocationRegion(
        VirtualRegion region)
    {
        if(region.Size == 0)
        {
            throw new ArgumentException(
                "Region cannot be empty.",
                nameof(region));
        }

        Region = region;

        _allocationPointer =
            region.Start;

        State =
            AllocationRegionState.Active;
    }

    public nint Allocate(nuint size)
    {
        if(State != AllocationRegionState.Active)
        {
            throw new InvalidOperationException(
                $"Cannot allocate from region in state {State}.");
        }

        if(size == 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        nint result =
            _allocationPointer;

        nint next =
            checked(result + (nint)size);

        if(next > Region.End)
        {
            return 0;
        }

        _allocationPointer =
            next;

        return result;
    }

    public void Retire()
    {
        if(State != AllocationRegionState.Active)
        {
            throw new InvalidOperationException(
                $"Cannot retire region in state {State}.");
        }

        State =
            AllocationRegionState.Retired;
    }

    public void BeginCompaction()
    {
        if(State != AllocationRegionState.Retired)
        {
            throw new InvalidOperationException(
                $"Cannot compact region in state {State}.");
        }

        State =
            AllocationRegionState.Compacting;
    }

    public void CompleteCompaction(nint usedEnd)
    {
        if(State != AllocationRegionState.Compacting)
        {
            throw new InvalidOperationException(
                $"Cannot complete compaction in state {State}.");
        }

        if(usedEnd < Region.Start ||
            usedEnd > Region.End)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usedEnd));
        }

        _allocationPointer =
            usedEnd;

        State =
            AllocationRegionState.Free;
    }

    public void AbortCompaction()
    {
        if(State != AllocationRegionState.Compacting)
        {
            throw new InvalidOperationException(
                $"Cannot abort compaction in state {State}.");
        }

        State =
            AllocationRegionState.Retired;
    }

    public void Reset()
    {
        if(State != AllocationRegionState.Free)
        {
            throw new InvalidOperationException(
                $"Cannot reset region in state {State}.");
        }

        _allocationPointer =
            Region.Start;

        State =
            AllocationRegionState.Active;
    }
}