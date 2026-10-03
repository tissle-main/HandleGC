namespace HandleGC;

public sealed class UpperAddressSpaceAllocator
{
    private readonly UpperAddressSpace _addressSpace;

    private nint _allocationPointer;

    public nint AllocationPointer =>
        _allocationPointer;

    public nuint RemainingSize =>
        (nuint)(
            _addressSpace.EndAddress -
            _allocationPointer);

    public UpperAddressSpaceAllocator(
        UpperAddressSpace addressSpace)
    {
        _addressSpace =
            addressSpace ??
            throw new ArgumentNullException(
                nameof(addressSpace));

        _allocationPointer =
            addressSpace.BaseAddress;
    }

    public VirtualRegion Allocate(
        nuint size)
    {
        if(size == 0)
            throw new ArgumentOutOfRangeException(
                nameof(size));

        nuint pageSize =
            _addressSpace.PageSize;

        size = AlignUp(size, pageSize);

        nint start =
            _allocationPointer;

        nint end =
            checked(start + (nint)size);

        if(end > _addressSpace.EndAddress)
        {
            throw new InvalidOperationException(
                "UpperAddressSpace is exhausted.");
        }

        _allocationPointer = end;

        return new VirtualRegion(
            start,
            size);
    }

    private static nuint AlignUp(
        nuint value,
        nuint alignment)
    {
        nuint remainder =
            value % alignment;

        if(remainder == 0)
            return value;

        return checked(
            value + alignment - remainder);
    }
}