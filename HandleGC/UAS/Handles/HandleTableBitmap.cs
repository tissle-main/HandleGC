using System.Numerics;

namespace HandleGC.UAS.Handles;

internal static unsafe class HandleTableBitmap
{
    internal const ulong ChunkCount = UpperAddressSpace.HandleTableBitmapSize * 8;
    internal const ulong WordCount = UpperAddressSpace.HandleTableBitmapSize / sizeof(ulong);
    internal const uint ChunksPerWord = 64;

    internal static bool MarkOccupied(uint chunkIndex)
    {
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        (ulong wordIndex, ulong bitIndex) = ulong.DivRem(chunkIndex, 64);

        ulong mask = 1UL << (int)bitIndex;
        ulong previous = Interlocked.And(ref words[wordIndex], ~mask);
        return (previous & mask) != 0;
    }
    internal static bool MarkFree(uint chunkIndex)
    {
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        (ulong wordIndex, ulong bitIndex) = ulong.DivRem(chunkIndex, 64);

        ulong mask = 1UL << (int)bitIndex;
        ulong previous = Interlocked.Or(ref words[wordIndex], mask);
        return (previous & mask) == 0;
    }
    internal static HandleTableChunk ReserveFreeChunk()
    {
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
            return new HandleTableChunk(UpperAddressSpace.HandleTable + globalHandleIndex, globalHandleIndex);
        }
        return default;
    }
}
