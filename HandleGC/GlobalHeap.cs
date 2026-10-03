using HandleGC;

public sealed class GlobalHeap
{
    private readonly VirtualRegion _region;
    private nint _allocationPointer;

    public VirtualRegion Region => _region;

    public nint AllocationPointer =>
        _allocationPointer;

    public nint UsedEnd =>
        _allocationPointer;

    public nuint UsedSize =>
        checked((nuint)(
            _allocationPointer - _region.Start));

    public nuint FreeSize =>
        checked((nuint)(
            _region.End - _allocationPointer));

    public GlobalHeap(
        VirtualRegion region)
    {
        if(region.Size == 0)
            throw new ArgumentException(
                "Global heap cannot be empty.",
                nameof(region));

        _region = region;
        _allocationPointer = region.Start;
    }

    public nint Allocate(nuint size)
    {
        if(size == 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        nint address =
            _allocationPointer;

        nint next =
            checked(address + (nint)size);

        if(next > _region.End)
            return 0;

        _allocationPointer = next;

        return address;
    }

    public void CompleteCompaction(nint usedEnd)
    {
        if(usedEnd < Region.Start || usedEnd > Region.End)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usedEnd),
                "Used end must be inside the GlobalHeap region.");
        }

        if(usedEnd > _allocationPointer)
        {
            throw new InvalidOperationException(
                "Compaction cannot increase GlobalHeap UsedEnd.");
        }

        _allocationPointer = usedEnd;
    }
}