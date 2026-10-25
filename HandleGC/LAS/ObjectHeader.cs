using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HandleGC.LAS;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ObjectHeader
{
    #region Static
    private const ulong AliveMask = 1UL << 63;
    internal const uint Size = 16;
    #endregion

    #region Instance
    private ulong rawValue;
    private uint length;
    private uint handleIndex;

    internal bool Alive
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (rawValue & AliveMask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if(value)
            {
                rawValue |= AliveMask;
            }
            else
            {
                rawValue &= ~AliveMask;
            }
        }
    }
    internal byte* TypeAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (byte*)(rawValue & ~AliveMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            rawValue = (rawValue & AliveMask) | ((ulong)value & ~AliveMask);
        }
    }
    internal uint Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            length = value;
        }
    }
    internal uint HandleIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => handleIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            handleIndex = value;
        }
    }
    #endregion

    #region Base
    public override readonly string ToString()
    {
        return $"[{(Alive ? "Alive" : "Dead")}|{(nuint)TypeAddress}|{Length}|{HandleIndex}]";
    }
    #endregion
}