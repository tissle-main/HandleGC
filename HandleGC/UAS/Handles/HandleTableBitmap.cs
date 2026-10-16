using System.Numerics;

namespace HandleGC.UAS.Handles;

internal static unsafe class HandleTableBitmap
{
    internal const ulong ChunkCount = UpperAddressSpace.HandleTableBitmapSize * 8;
    internal const ulong WordCount = UpperAddressSpace.HandleTableBitmapSize / sizeof(ulong);

    internal static HandleTableChunk FindFirstFreeChunk()
    {
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        for(uint index = 0; index < WordCount; index++)
        {
            ulong word = words[index];
            if(word == 0)
            {
                continue;
            }

            uint chunkIndex = (uint)BitOperations.TrailingZeroCount(word);
            uint baseCellIndex = chunkIndex * HandleTableChunk.CellCount;
            return new HandleTableChunk(UpperAddressSpace.HandleTable + baseCellIndex, baseCellIndex);
        }
        return default;
    }
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
}