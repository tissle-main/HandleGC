using System.Runtime.InteropServices;

namespace HandleGC.LAS;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ObjectHeader
{
    #region Static
    private const ulong AliveMask       = 0b10000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000;
    private const ulong TypeAddressMask = 0b00000000_00000000_11111111_11111111_11111111_11111111_11111111_11111111;
    #endregion

    #region Instance
    private ulong rawValue;
    private uint length;
    private uint handleIndex;

    internal bool Alive
    {
        readonly get => (rawValue & AliveMask) != 0;
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
        readonly get => (byte*)(rawValue & TypeAddressMask);
        set
        {
            rawValue = (rawValue & ~TypeAddressMask) | ((ulong)value & TypeAddressMask);
        }
    }
    internal uint Length
    {
        readonly get => length;
        set
        {
            length = value;
        }
    }
    internal uint HandleIndex
    {
        readonly get => handleIndex;
        set
        {
            handleIndex = value;
        }
    }
    #endregion
}