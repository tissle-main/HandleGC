using System.Numerics;

namespace HandleGC.UAS.Handles;

/// <summary>
/// Controls handle table bitmap
/// </summary>
internal static unsafe class HandleTableBitmap
{
    internal const ulong ChunkCount = UpperAddressSpace.HandleTableBitmapSize * 8;
    internal const ulong WordCount = UpperAddressSpace.HandleTableBitmapSize / sizeof(ulong);
    internal const uint ChunksPerWord = 64;

    /// <summary>
    /// Marks the corresponding chunk as reserved
    /// </summary>
    /// <param name="chunkIndex">Chunk index</param>
    /// <returns>True if current call reserved a chunk. Otherwise another thread has already done it</returns>
    /// <remarks>
    /// Method is thread-safe
    /// </remarks>
    internal static bool MarkOccupied(uint chunkIndex)
    {
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        (ulong wordIndex, ulong bitIndex) = ulong.DivRem(chunkIndex, 64);

        ulong mask = 1UL << (int)bitIndex;
        ulong previous = Interlocked.And(ref words[wordIndex], ~mask);
        return (previous & mask) != 0;
    }

    /// <summary>
    /// Marks the corresponding chunk as free
    /// </summary>
    /// <param name="chunkIndex">Chunk index</param>
    /// <remarks>
    /// Method is thread-safe
    /// </remarks>
    internal static void MarkFree(uint chunkIndex)
    {
        //Marking chunk as free without any sync is safe
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        (ulong wordIndex, ulong bitIndex) = ulong.DivRem(chunkIndex, 64);
        words[wordIndex] |= 1UL << (int)bitIndex;
    }

    /// <summary>
    /// Finds free chunk and reserves it
    /// </summary>
    /// <param name="success">Contains true if chunk was reserved successfully. Otherwise, contains false</param>
    /// <returns>Reserved chunk of handles</returns>
    /// <remarks>
    /// Method is thread-safe
    /// </remarks>
    internal static HandleTableChunk ReserveFreeChunk(out bool success)
    {
        //Each bit (1) marks the corresponding chunk as having free handles
        //Word contain 64 bits, so we can check if one of 64 chunks is free
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        for(uint wordIndex = 0; wordIndex < WordCount; wordIndex++)
        {
            ulong word = words[wordIndex];
            if(word == 0)
            {
                continue;
            }

            uint localChunkIndex = (uint)BitOperations.TrailingZeroCount(word);
            uint globalChunkIndex = wordIndex * ChunksPerWord + localChunkIndex;
            if(!MarkOccupied(globalChunkIndex))
            {
                wordIndex -= 1;
                continue;
            }
            uint globalHandleIndex = globalChunkIndex * HandleTableChunk.HandleCount;

            success = true;
            return new HandleTableChunk(UpperAddressSpace.HandleTable + globalHandleIndex, globalHandleIndex);
        }

        success = false;
        return default;
    }
}