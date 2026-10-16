using System.Runtime.InteropServices;

namespace HandleGC.UAS.Heap;

[StructLayout(LayoutKind.Sequential)]
internal struct MemoryBlockHeader
{
    #region Static
    private const ulong AliveMask = 1UL << 63;
    internal const ulong Size = 16;
    #endregion

    #region Instance
    private ulong currentHeader;
    private ulong previousHeader;

    internal bool CurrentAlive
    {
        readonly get => (currentHeader & AliveMask) != 0;
        set
        {
            if(value)
            {
                currentHeader |= AliveMask;
            }
            else
            {
                currentHeader &= ~AliveMask;
            }
        }
    }
    internal ulong CurrentLength
    {
        readonly get => currentHeader & ~AliveMask;
        set
        {
            currentHeader = value | (currentHeader & AliveMask);
        }
    }
    internal bool PreviousAlive
    {
        readonly get => (previousHeader & AliveMask) != 0;
        set
        {
            if(value)
            {
                previousHeader |= AliveMask;
            }
            else
            {
                previousHeader &= ~AliveMask;
            }
        }
    }
    internal ulong PreviousLength
    {
        readonly get => previousHeader & ~AliveMask;
        set
        {
            previousHeader = value | (previousHeader & AliveMask);
        }
    }
    #endregion
}