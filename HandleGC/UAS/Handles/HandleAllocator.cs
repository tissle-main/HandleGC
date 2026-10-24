namespace HandleGC.UAS.Handles;

internal static unsafe class HandleAllocator
{
    internal static uint Allocate(HardwareThread* thread, byte* objAddress, out bool success)
    {
        while(true)
        {
            uint handleIndex = thread->handleTableChunk.Allocate(objAddress, out success);
            if(success)
            {
                return handleIndex;
            }

            thread->handleTableChunk = HandleTableBitmap.ReserveFreeChunk(out success);
            if(!success)
            {
                throw new OutOfMemoryException("No free chunks available in handle table");
            }
        }
    }
    internal static void Free(uint cellIndex)
    {
        UpperAddressSpace.HandleTable[cellIndex] = 0;
    }
}