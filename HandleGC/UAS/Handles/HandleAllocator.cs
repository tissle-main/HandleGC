namespace HandleGC.UAS.Handles;

internal static unsafe class HandleAllocator
{
    internal static uint Allocate(HardwareThread* thread, byte* objAddress, out bool success)
    {
        HandleTableChunk chunk = thread->handleTableChunk;
        uint cellIndex = chunk.Allocate(objAddress, out success);
        if(success)
        {
            return cellIndex;
        }
        chunk = HandleTableBitmap.ReserveFreeChunk();
        if(chunk.IsNull)
        {
            throw new OutOfMemoryException("No free chunks available in handle table");
        }
        thread->handleTableChunk = chunk;

        return chunk.Allocate(objAddress, out success);
    }
    internal static void Free(uint cellIndex)
    {
        UpperAddressSpace.HandleTable[cellIndex] = 0;
    }
}