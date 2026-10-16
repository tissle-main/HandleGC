using System.Runtime.InteropServices;

namespace HandleGC.UAS.Handles;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Handle(ulong thisRawValue)
{
    #region Static
    private const ulong AddressMask    = 0b00000000_00000000_01111111_11111111_11111111_11111111_11111111_11111111;
    private const ulong UsageCountMask = 0b00111111_11111111_10000000_00000000_00000000_00000000_00000000_00000000;
    private const ulong GCLockedMask   = 0b01000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000;
    private const ulong RSFlagMask     = 0b10000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000;
    private const int AddressLength = 47;
    private const int UsageCountShift = AddressLength;
    #endregion

    #region Instance
    internal ulong RawValue
    {
        get => thisRawValue;
    }
    internal byte* Address
    {
        readonly get => (byte*)(thisRawValue & AddressMask);
        set
        {
            thisRawValue = (thisRawValue & ~AddressMask) | ((ulong)value & AddressMask);
        }
    }
    internal ulong UsageCount
    {
        readonly get => (thisRawValue & UsageCountMask) >> UsageCountShift;
        set
        {
            thisRawValue = (thisRawValue & ~UsageCountMask) | ((value << UsageCountShift) & UsageCountMask);
        }
    }
    internal bool GCLocked
    {
        readonly get => (thisRawValue & GCLockedMask) != 0;
        set
        {
            if(value)
            {
                thisRawValue |= GCLockedMask;
            }
            else
            {
                thisRawValue &= ~GCLockedMask;
            }
        }
    }
    internal bool RSFlag
    {
        readonly get => (thisRawValue & RSFlagMask) != 0;
        set
        {
            if(value)
            {
                thisRawValue |= RSFlagMask;
            }
            else
            {
                thisRawValue &= ~RSFlagMask;
            }
        }
    }
    #endregion
}