namespace HandleGC.UAS.Heap;

internal static unsafe class UnmanagedHeap
{
    private static readonly Lock Sync = new();

    internal static MemoryBlock Allocate(ulong pageCount)
    {
        ArgumentOutOfRangeException.ThrowIfZero(pageCount);
        ulong requestedLength = checked(pageCount * UpperAddressSpace.MemoryPageSize + MemoryBlockHeader.Size);
        ulong blockLength = checked((requestedLength + UpperAddressSpace.MemoryPageSize - 1) / UpperAddressSpace.MemoryPageSize * UpperAddressSpace.MemoryPageSize);
        lock(Sync)
        {
            byte* currentAddress = UpperAddressSpace.UnmanagedHeap;
            byte* heapEnd = currentAddress + UpperAddressSpace.UnmanagedHeapSize;
            while(currentAddress < heapEnd)
            {
                var currentHeader = (MemoryBlockHeader*)currentAddress;
                ulong currentLength = currentHeader->CurrentLength;
                if(!currentHeader->CurrentAlive && currentLength >= blockLength)
                {
                    currentHeader->CurrentAlive = true;
                    ulong remainingLength = currentLength - blockLength;
                    if(remainingLength >= MemoryBlockHeader.Size)
                    {
                        currentHeader->CurrentLength = blockLength;
                        var nextHeader = (MemoryBlockHeader*)(currentAddress + blockLength);
                        nextHeader->CurrentAlive = false;
                        nextHeader->CurrentLength = remainingLength;
                        nextHeader->PreviousAlive = true;
                        nextHeader->PreviousLength = blockLength;

                        byte* nextAddress = currentAddress + currentLength;
                        if(nextAddress < heapEnd)
                        {
                            var thirdHeader = (MemoryBlockHeader*)nextAddress;
                            thirdHeader->PreviousLength = remainingLength;
                        }
                    }
                    else
                    {
                        byte* nextAddress = currentAddress + currentLength;
                        if(nextAddress < heapEnd)
                        {
                            var nextHeader = (MemoryBlockHeader*)nextAddress;
                            nextHeader->PreviousAlive = true;
                        }
                    }
                    return new MemoryBlock(currentAddress);
                }
                currentAddress += currentLength;
            }
        }
        throw new OutOfMemoryException("Upper address space does not contain a sufficiently large free block.");
    }
    internal static void Free(MemoryBlock block)
    {
        lock(Sync)
        {
            MemoryBlockHeader* baseHeader = block.Header;
            if(!baseHeader->CurrentAlive)
            {
                return;
            }
            byte* heapStart = UpperAddressSpace.UnmanagedHeap;
            byte* heapEnd = heapStart + UpperAddressSpace.UnmanagedHeapSize;
            byte* baseAddress = block.Address;
            baseHeader->CurrentAlive = false;

            if(baseAddress > heapStart && !baseHeader->PreviousAlive)
            {
                byte* previousAddress = baseAddress - baseHeader->PreviousLength;
                MemoryBlockHeader* previousHeader = (MemoryBlockHeader*)previousAddress;
                previousHeader->CurrentLength += baseHeader->CurrentLength;
                baseAddress = previousAddress;
                baseHeader = previousHeader;
            }

            while(true)
            {
                byte* nextAddress = baseAddress + baseHeader->CurrentLength;
                if(nextAddress >= heapEnd)
                    return;

                var nextHeader = (MemoryBlockHeader*)nextAddress;
                if(nextHeader->CurrentAlive)
                {
                    nextHeader->PreviousAlive = false;
                    nextHeader->PreviousLength = baseHeader->CurrentLength;
                    return;
                }
                baseHeader->CurrentLength += nextHeader->CurrentLength;
            }
        }
    }
    internal static void Init()
    {
        var header = (MemoryBlockHeader*)UpperAddressSpace.UnmanagedHeap;
        header->CurrentAlive = false;
        header->CurrentLength = UpperAddressSpace.UnmanagedHeapSize;
        header->PreviousAlive = false;
        header->PreviousLength = 0;
    }
}
