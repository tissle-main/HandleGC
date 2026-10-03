using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HandleGC;

public sealed class LowerAddressSpace : IDisposable
{
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_DECOMMIT = 0x4000;
    private const uint MEM_RELEASE = 0x8000;

    private const uint PAGE_NOACCESS = 0x01;
    private const uint PAGE_READWRITE = 0x04;

    public nint BaseAddress { get; }
    public nuint Size { get; }

    public nint EndAddress =>
        BaseAddress + checked((nint)Size);

    public nuint PageSize { get; }

    private bool _disposed;

    public LowerAddressSpace(
        nuint size,
        nint preferredBase = 0)
    {
        if(size == 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        PageSize = (nuint)Environment.SystemPageSize;

        size = AlignUp(size, PageSize);

        nint address = VirtualAlloc(
            preferredBase,
            size,
            MEM_RESERVE,
            PAGE_NOACCESS);

        if(address == 0)
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to reserve virtual address space.");

        // Якщо ми явно попросили конкретну адресу,
        // ОС повинна видати саме її.
        if(preferredBase != 0 && address != preferredBase)
        {
            VirtualFree(address, 0, MEM_RELEASE);

            throw new InvalidOperationException(
                $"Could not reserve the requested address. " +
                $"Requested: 0x{preferredBase:X}, " +
                $"Actual: 0x{address:X}");
        }

        BaseAddress = address;
        Size = size;
    }

    public void Commit(nint address, nuint size)
    {
        ThrowIfDisposed();

        ValidateRange(address, size);
        ValidatePageAlignment(address, size);

        nint result = VirtualAlloc(
            address,
            size,
            MEM_COMMIT,
            PAGE_READWRITE);

        if(result == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to commit virtual memory.");
        }
    }

    public void Decommit(nint address, nuint size)
    {
        ThrowIfDisposed();

        ValidateRange(address, size);
        ValidatePageAlignment(address, size);

        if(!VirtualFree(
                address,
                size,
                MEM_DECOMMIT))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to decommit virtual memory.");
        }
    }

    public void Dispose()
    {
        if(_disposed)
            return;

        if(!VirtualFree(
                BaseAddress,
                0,
                MEM_RELEASE))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to release virtual address space.");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ValidateRange(nint address, nuint size)
    {
        if(size == 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        nuint offset;

        try
        {
            offset = checked(
                (nuint)(address - BaseAddress));
        }
        catch(OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(address));
        }

        if(offset >= Size)
            throw new ArgumentOutOfRangeException(nameof(address));

        if(size > Size - offset)
            throw new ArgumentOutOfRangeException(nameof(size));
    }

    private void ValidatePageAlignment(
        nint address,
        nuint size)
    {
        if((nuint)address % PageSize != 0)
        {
            throw new ArgumentException(
                "Address must be page-aligned.",
                nameof(address));
        }

        if(size % PageSize != 0)
        {
            throw new ArgumentException(
                "Size must be a multiple of the page size.",
                nameof(size));
        }
    }

    private static nuint AlignUp(nuint value, nuint alignment)
    {
        nuint remainder = value % alignment;

        if(remainder == 0)
            return value;

        return checked(value + alignment - remainder);
    }

    public bool Contains(VirtualRegion region)
    {
        return region.Start >= BaseAddress &&
               region.End <= EndAddress;
    }

    private void ThrowIfDisposed()
    {
        if(_disposed)
            throw new ObjectDisposedException(
                nameof(LowerAddressSpace));
    }

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    private static extern nint VirtualAlloc(
        nint lpAddress,
        nuint dwSize,
        uint flAllocationType,
        uint flProtect);

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(
        nint lpAddress,
        nuint dwSize,
        uint dwFreeType);
}