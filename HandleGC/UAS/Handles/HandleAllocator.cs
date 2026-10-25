namespace HandleGC.UAS.Handles;

internal static unsafe class HandleAllocator
{
    internal static uint Allocate(HandleTableChunk* handleTableChunk, byte* objAddress, out bool success)
    {
        while(true)
        {
            uint handleIndex = handleTableChunk->Allocate(objAddress, out success);
            if(success)
            {
                return handleIndex;
            }

            *handleTableChunk = HandleTableBitmap.ReserveFreeChunk(out success);
            if(!success)
            {
                throw new OutOfMemoryException("No free chunks available in handle table");
            }
        }
    }
}