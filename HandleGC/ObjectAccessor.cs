using System.Runtime.CompilerServices;

namespace HandleGC;

public unsafe sealed class ObjectAccessor
{
    private readonly HandleTable _handleTable;

    public ObjectAccessor(HandleTable handleTable)
    {
        _handleTable = handleTable
            ?? throw new ArgumentNullException(nameof(handleTable));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GetAddressFast(Handle handle)
    {
        return handle.Address;
    }

    public bool TryAcquire(
        uint handleIndex,
        out ObjectAccess access)
        {
            HandleEntryRef entry =
                _handleTable.GetEntry(handleIndex);

            if(!entry.TryAcquire())
            {
                access = default;
                return false;
            }

            nint address = entry.Read().Address;

            access = new ObjectAccess(
                entry,
                address);

            return true;
        }
}