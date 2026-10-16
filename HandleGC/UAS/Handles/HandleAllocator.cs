namespace HandleGC.UAS.Handles;

internal static unsafe class HandleAllocator
{
    internal static uint Allocate(HardwareThread* thread, byte* objAddress, out bool success)
    {
        uint cellIndex = thread->handleTableChunk.Allocate(objAddress, out success);
        if(success)
        {
            return cellIndex;
        }

        HandleTableChunk chunk;
        do
        {
            chunk = HandleTableBitmap.FindFirstFreeChunk();
            if(chunk.IsDefault)
            {
                throw new OutOfMemoryException("No free chunks available in handle table");
            }
        }
        while(!HandleTableBitmap.MarkOccupied(chunk.Index));
        thread->handleTableChunk = chunk;

        return thread->handleTableChunk.Allocate(objAddress, out success);
    }
    internal static void Free(uint cellIndex)
    {
        *(ulong*)(UpperAddressSpace.HandleTable + cellIndex) = 0;
    }
}