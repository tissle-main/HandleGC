#define VECTOR256

using System.Numerics;
using System.Runtime.CompilerServices;
#if VECTOR512 || VECTOR256 || VECTOR128
using System.Runtime.Intrinsics;
#endif

namespace HandleGC.UAS.Handles;

/// <summary>
/// Controls 64 sequencial handles in the table
/// </summary>
/// <param name="firstHandle">Address of the first handle</param>
/// <param name="firstHandleIndex">Handle index of the first handle</param>
/// <remarks>
/// Allocation is not thread-safe, but editing and zeroing of the allocated handles is thread-safe
/// </remarks>
internal unsafe struct HandleTableChunk(ulong* firstHandle, uint firstHandleIndex)
{
    #region Static
    internal const uint HandleCount = 64; 
    #endregion

    #region Instance
    private readonly ulong* firstHandle = firstHandle;
    private readonly uint firstHandleIndex = firstHandleIndex;

    /// <summary>
    /// Each bit (1) marks the corresponding handle as free
    /// </summary>
    private ulong freeHandleBitmap = 0;

    internal readonly bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => firstHandle is null;
    }
    internal readonly bool IsFull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => freeHandleBitmap == 0;
    }
    internal readonly uint ChunkIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => firstHandleIndex / HandleCount;
    }

    /// <summary>
    /// Synchronizes <see cref="freeHandleBitmap" /> with the actual state of the handles
    /// </summary>
    /// <returns>True if the chunk contains free handles. Otherwise, returns false</returns>
    internal bool Scan()
    {
        ulong newBitmap = 0;
        ulong* localHandleTable = firstHandle;
#if VECTOR512
        for(int index = 0; index < HandleCount; index += 8)
        {
            Vector512<ulong> vector = Vector512.Load(localHandleTable + index);
            ulong bitmap = Vector512.Equals(vector, Vector512<ulong>.Zero).ExtractMostSignificantBits();
            newBitmap |= bitmap << index;
        }
#elif VECTOR256
        for(int index = 0; index < HandleCount; index += 4)
        {
            Vector256<ulong> vector = Vector256.Load(localHandleTable + index);
            ulong bitmap = Vector256.Equals(vector, Vector256<ulong>.Zero).ExtractMostSignificantBits();
            newBitmap |= bitmap << index;
        }
#elif VECTOR128
        for(int index = 0; index < HandleCount; index += 2)
        {
            Vector128<ulong> vector = Vector128.Load(localHandleTable + index);
            ulong bitmap = Vector128.Equals(vector, Vector128<ulong>.Zero).ExtractMostSignificantBits();
            newBitmap |= bitmap << index;
        }
#else
        for(int index = 0; index < HandleCount; index += 1)
        {
            if(localHandleTable[index] == 0)
            {
                newBitmap |= 1UL << index;
            }
        }
#endif
        freeHandleBitmap = newBitmap;
        return !IsFull;
    }

    /// <summary>
    /// Reserves a handle in the chunk
    /// </summary>
    /// <param name="objAddress">Address of the object</param>
    /// <param name="success">Contains true if handle was reserved successfully. Otherwise, contains false</param>
    /// <returns></returns>
    internal uint Allocate(byte* objAddress, out bool success)
    {
        if(IsFull && !Scan())
        {
            success = false;
            return default;
        }
        success = true;

        int localHandleIndex = BitOperations.TrailingZeroCount(freeHandleBitmap);
        uint globalHandleIndex = firstHandleIndex + (uint)localHandleIndex;

        HandleTable.SetObjectAddress(globalHandleIndex, objAddress);
        freeHandleBitmap &= ~(1UL << localHandleIndex);

        return globalHandleIndex;
    }
#endregion
}