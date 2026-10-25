namespace HandleGC.UAS.Handles;

/// <summary>
/// Provides way to reserve handles
/// </summary>
internal static unsafe class HandleAllocator
{
    /// <summary>
    /// Reserves a handle. If provided chunk has no free space, replaces it with a new one
    /// </summary>
    /// <param name="handleTableChunk">Handle table chunk</param>
    /// <param name="objAddress">Address of the object</param>
    /// <param name="success">Contains true if handle was reserved successfully. Otherwise, contains false</param>
    /// <returns>Index of the reserved handle</returns>
    /// <exception cref="OutOfMemoryException">Throws if provided chunk is full and we can`t get a new one</exception>
    /// <remarks>
    /// Method is thread-safe
    /// </remarks>
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