using System.Numerics;
using System.Runtime.Intrinsics;

namespace HandleGC.UAS.Handles;

internal unsafe struct HandleTableChunk(Handle* startAddress, uint baseCellIndex)
{
    #region Static
    internal const uint CellCount = 64; 
    #endregion

    #region Instance
    private readonly Handle* startAddress = startAddress;
    private readonly uint baseCellIndex = baseCellIndex;
    private ulong freeCellBitmap = 0;

    internal readonly bool IsDefault
    {
        get => startAddress is null;
    }
    internal readonly bool IsFull
    {
        get => freeCellBitmap == 0;
    }
    internal readonly uint Index
    {
        get => baseCellIndex / CellCount;
    }

    internal bool Scan()
    {
        ulong newBitmap = 0;
        ulong* handleTable = (ulong*)startAddress;
        if(Vector512.IsHardwareAccelerated)
        {
            for(int index = 0; index < CellCount; index += 8)
            {
                Vector512<ulong> vector = Vector512.Load(handleTable + index);
                ulong bitmap = Vector512.Equals(vector, Vector512<ulong>.Zero).ExtractMostSignificantBits();
                newBitmap |= bitmap << index;
            }
        }
        else if(Vector256.IsHardwareAccelerated)
        {
            for(int index = 0; index < CellCount; index += 4)
            {
                Vector256<ulong> vector = Vector256.Load(handleTable + index);
                ulong bitmap = Vector256.Equals(vector, Vector256<ulong>.Zero).ExtractMostSignificantBits();
                newBitmap |= bitmap << index;
            }
        }
        else if(Vector128.IsHardwareAccelerated)
        {
            for(int index = 0; index < CellCount; index += 2)
            {
                Vector128<ulong> vector = Vector128.Load(handleTable + index);
                ulong bitmap = Vector128.Equals(vector, Vector128<ulong>.Zero).ExtractMostSignificantBits();
                newBitmap |= bitmap << index;
            }
        }
        else
        {
            for(int index = 0; index < CellCount; index += 1)
            {
                if(handleTable[index] == 0)
                {
                    newBitmap |= 1UL << index;
                }
            }
        }
        freeCellBitmap = newBitmap;
        return !IsFull;
    }
    internal uint Allocate(byte* objAddress, out bool success)
    {
        if(IsFull && !Scan())
        {
            success = false;
            return default;
        }
        success = true;

        int index = BitOperations.TrailingZeroCount(freeCellBitmap);
        freeCellBitmap &= ~(1UL << index);
        startAddress[index] = new Handle()
        {
            Address = objAddress
        };
        return baseCellIndex + (uint)index;
    }
    #endregion
}