namespace HandleGC;

public unsafe sealed class GlobalHeapScanner
{
    public delegate void ObjectVisitor(
        nint objectAddress,
        ObjectHeader* header);

    public void Scan(
        GlobalHeap heap,
        ObjectVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        nint current =
            heap.Region.Start;

        nint end =
            heap.UsedEnd;

        while(current < end)
        {
            ObjectHeader* header =
                (ObjectHeader*)current;

            uint length =
                header->Length;

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
                    $"Object at 0x{current:X} exceeds GlobalHeap boundary.");
            }

            visitor(
                current,
                header);

            current = next;
        }
    }
}