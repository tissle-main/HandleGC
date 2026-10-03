namespace HandleGC;

public unsafe sealed class HeapScanner
{
    public delegate void ObjectVisitor(
        nint objectAddress,
        ObjectHeader* header);

    public void Scan(
        AllocationRegion region,
        ObjectVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        nint current =
            region.Region.Start;

        nint end = region.UsedEnd;

        while(current < end)
        {
            ObjectHeader* header =
                (ObjectHeader*)current;

            uint length =
                header->Length;

            if(length == 0)
                break;

            if(length < ObjectHeader.Size)
            {
                throw new InvalidOperationException(
                    $"Invalid object length at 0x{current:X}: {length}");
            }

            nint next =
                checked(current + (nint)length);

            if(next > end)
            {
                throw new InvalidOperationException(
                    $"Object at 0x{current:X} exceeds used region boundary.");
            }

            visitor(current, header);

            current = next;
        }
    }
}