using HandleGC;

const nuint lowerSize = 64 * 1024 * 1024;
const nuint upperSize = 16 * 1024 * 1024;

// ============================================================
// Address Spaces
// ============================================================

using var lower =
    new LowerAddressSpace(lowerSize);

using var upper =
    new UpperAddressSpace(upperSize);

// ============================================================
// Upper Address Space
// ============================================================

var upperAllocator =
    new UpperAddressSpaceAllocator(upper);

// ============================================================
// Handle Table
// ============================================================

const uint handleCapacity = 1024;

VirtualRegion handleRegion =
    upperAllocator.Allocate(
        (nuint)handleCapacity * HandleTable.EntrySize);

var handleTable =
    new HandleTable(
        upper,
        handleRegion,
        handleCapacity);

// ============================================================
// Runtime Components
// ============================================================

var gc =
    new GarbageCollector(handleTable);

var scanner =
    new HeapScanner();

var compactor =
    new HeapCompactor(gc);

var globalHeapCompactor =
    new GlobalHeapCompactor(gc);

// ============================================================
// Lower Address Space Layout
// ============================================================
//
// LOW
//
// ┌─────────────────────────────────────┐
// │ Global Heap                         │
// │ 4 MB                                │
// ├─────────────────────────────────────┤
// │ Evacuation AllocationRegion         │
// │ 1 MB                                │
// ├─────────────────────────────────────┤
// │ Global Heap Compaction Test Heap    │
// │ 1 MB                                │
// └─────────────────────────────────────┘
//
// HIGH
//
// ============================================================

// ------------------------------------------------------------
// Global Heap
// ------------------------------------------------------------

const nuint globalHeapSize =
    4 * 1024 * 1024;

VirtualRegion globalHeapRegion =
    new VirtualRegion(
        lower.BaseAddress,
        globalHeapSize);

lower.Commit(
    globalHeapRegion.Start,
    globalHeapRegion.Size);

var globalHeap =
    new GlobalHeap(
        globalHeapRegion);

// ------------------------------------------------------------
// Compaction Test Region
// ------------------------------------------------------------

const nuint compactionRegionSize =
    1024 * 1024;

VirtualRegion compactionRegion =
    new VirtualRegion(
        globalHeapRegion.End,
        compactionRegionSize);

lower.Commit(
    compactionRegion.Start,
    compactionRegion.Size);

var compactionAllocation =
    new AllocationRegion(
        compactionRegion);

var compactionAllocator =
    new ObjectAllocator(
        compactionAllocation,
        handleTable);

// ------------------------------------------------------------
// Evacuation Test Region
// ------------------------------------------------------------

const nuint evacuationRegionSize =
    1024 * 1024;

VirtualRegion evacuationRegion =
    new VirtualRegion(
        compactionRegion.End,
        evacuationRegionSize);

lower.Commit(
    evacuationRegion.Start,
    evacuationRegion.Size);

var evacuationAllocation =
    new AllocationRegion(
        evacuationRegion);

var evacuationAllocator =
    new ObjectAllocator(
        evacuationAllocation,
        handleTable);

var evacuator =
    new RegionEvacuator(
        globalHeap,
        gc,
        scanner,
        handleTable);

// ------------------------------------------------------------
// Global Heap Compaction Test Heap
// ------------------------------------------------------------

const nuint globalCompactionHeapSize =
    1024 * 1024;

VirtualRegion globalCompactionRegion =
    new VirtualRegion(
        evacuationRegion.End,
        globalCompactionHeapSize);

lower.Commit(
    globalCompactionRegion.Start,
    globalCompactionRegion.Size);

var globalCompactionHeap =
    new GlobalHeap(
        globalCompactionRegion);

// ------------------------------------------------------------
// Concurrent Access Test Region
// ------------------------------------------------------------

const nuint concurrentRegionSize =
    1024 * 1024;

VirtualRegion concurrentRegion =
    new VirtualRegion(
        globalCompactionRegion.End,
        concurrentRegionSize);

lower.Commit(
    concurrentRegion.Start,
    concurrentRegion.Size);

var concurrentAllocation =
    new AllocationRegion(
        concurrentRegion);

var concurrentAllocator =
    new ObjectAllocator(
        concurrentAllocation,
        handleTable);

// ============================================================
// Tests
// ============================================================

TestCompaction(
    compactionAllocator,
    handleTable,
    scanner,
    compactor,
    compactionAllocation);

TestRegionEvacuation(
    evacuationAllocator,
    evacuationAllocation,
    globalHeap,
    evacuator,
    handleTable,
    scanner);

TestGlobalHeapCompaction(
    handleTable,
    globalCompactionHeap,
    globalHeapCompactor);

TestConcurrentAccess(
    concurrentAllocator,
    handleTable);

// ============================================================
// Compaction Test
// ============================================================

static unsafe void TestCompaction(
    ObjectAllocator allocator,
    HandleTable handleTable,
    HeapScanner scanner,
    HeapCompactor compactor,
    AllocationRegion allocation)
{
    Console.WriteLine();
    Console.WriteLine("=== Compaction Test ===");
    Console.WriteLine();

    // --------------------------------------------------------
    // Allocate objects
    // --------------------------------------------------------

    Handle objectA =
        allocator.Allocate(32);

    Handle objectB =
        allocator.Allocate(64);

    Handle objectC =
        allocator.Allocate(128);

    Handle objectD =
        allocator.Allocate(48);

    nint addressA =
        objectA.Address;

    nint addressB =
        objectB.Address;

    nint addressC =
        objectC.Address;

    nint addressD =
        objectD.Address;

    // --------------------------------------------------------
    // Initialize data
    // --------------------------------------------------------

    FillObject(objectA, 0xAA);
    FillObject(objectB, 0xBB);
    FillObject(objectC, 0xCC);
    FillObject(objectD, 0xDD);

    // --------------------------------------------------------
    // Mark B and D as dead
    // --------------------------------------------------------

    ((ObjectHeader*)addressB)->Alive = false;
    ((ObjectHeader*)addressD)->Alive = false;

    Console.WriteLine("Before compaction:");

    PrintHeap(
        scanner,
        allocation);

    Console.WriteLine();

    // --------------------------------------------------------
    // Save information about live objects
    // --------------------------------------------------------

    uint handleIndexA =
        ((ObjectHeader*)addressA)->HandleIndex;

    uint handleIndexC =
        ((ObjectHeader*)addressC)->HandleIndex;

    uint lengthA =
        ((ObjectHeader*)addressA)->Length;

    uint lengthC =
        ((ObjectHeader*)addressC)->Length;

    // --------------------------------------------------------
    // Retire region
    // --------------------------------------------------------

    allocation.Retire();

    // --------------------------------------------------------
    // Compact
    // --------------------------------------------------------

    nint freeStart =
        compactor.Compact(
            allocation);

    Console.WriteLine("After compaction:");

    PrintHeap(
        scanner,
        allocation);

    Console.WriteLine();

    Console.WriteLine(
        $"Free start: 0x{freeStart:X}");

    // --------------------------------------------------------
    // Read updated handles
    // --------------------------------------------------------

    Handle movedA =
        handleTable.Read(
            handleIndexA);

    Handle movedC =
        handleTable.Read(
            handleIndexC);

    // --------------------------------------------------------
    // Verify A
    // --------------------------------------------------------

    if(movedA.Address != addressA)
    {
        throw new InvalidOperationException(
            "Object A should not have moved.");
    }

    // --------------------------------------------------------
    // Verify C
    // --------------------------------------------------------

    if(movedC.Address == addressC)
    {
        throw new InvalidOperationException(
            "Object C should have moved.");
    }

    nint expectedCAddress =
        addressA + (nint)lengthA;

    if(movedC.Address != expectedCAddress)
    {
        throw new InvalidOperationException(
            "Object C was moved to an unexpected address.");
    }

    // --------------------------------------------------------
    // Verify headers
    // --------------------------------------------------------

    ObjectHeader* headerA =
        (ObjectHeader*)movedA.Address;

    ObjectHeader* headerC =
        (ObjectHeader*)movedC.Address;

    if(headerA->HandleIndex != handleIndexA)
    {
        throw new InvalidOperationException(
            "Object A handle index is invalid.");
    }

    if(headerC->HandleIndex != handleIndexC)
    {
        throw new InvalidOperationException(
            "Object C handle index is invalid.");
    }

    if(headerA->Length != lengthA)
    {
        throw new InvalidOperationException(
            "Object A length changed.");
    }

    if(headerC->Length != lengthC)
    {
        throw new InvalidOperationException(
            "Object C length changed.");
    }

    // --------------------------------------------------------
    // Verify data
    // --------------------------------------------------------

    VerifyObject(
        movedA,
        0xAA);

    VerifyObject(
        movedC,
        0xCC);

    // --------------------------------------------------------
    // Verify handle state
    // --------------------------------------------------------

    if(movedA.UsageCount != 0 ||
        movedA.GCLocked ||
        movedA.RequiresSync)
    {
        throw new InvalidOperationException(
            "Object A handle is not in normal state.");
    }

    if(movedC.UsageCount != 0 ||
        movedC.GCLocked ||
        movedC.RequiresSync)
    {
        throw new InvalidOperationException(
            "Object C handle is not in normal state.");
    }

    Console.WriteLine(
        "Compaction test passed.");
}

// ============================================================
// Region Evacuation Test
// ============================================================

static unsafe void TestRegionEvacuation(
    ObjectAllocator allocator,
    AllocationRegion allocation,
    GlobalHeap globalHeap,
    RegionEvacuator evacuator,
    HandleTable handleTable,
    HeapScanner scanner)
{
    Console.WriteLine();
    Console.WriteLine("=== Region Evacuation Test ===");

    // --------------------------------------------------------
    // Allocate objects
    // --------------------------------------------------------

    Handle objectA =
        allocator.Allocate(32);

    Handle objectB =
        allocator.Allocate(64);

    Handle objectC =
        allocator.Allocate(128);

    Handle objectD =
        allocator.Allocate(48);

    nint addressA =
        objectA.Address;

    nint addressC =
        objectC.Address;

    // --------------------------------------------------------
    // Initialize data
    // --------------------------------------------------------

    FillObject(objectA, 0xAA);
    FillObject(objectB, 0xBB);
    FillObject(objectC, 0xCC);
    FillObject(objectD, 0xDD);

    // --------------------------------------------------------
    // Save handle indexes
    // --------------------------------------------------------

    uint handleA =
        ((ObjectHeader*)objectA.Address)->HandleIndex;

    uint handleC =
        ((ObjectHeader*)objectC.Address)->HandleIndex;

    // --------------------------------------------------------
    // Mark dead objects
    // --------------------------------------------------------

    ((ObjectHeader*)objectB.Address)->Alive = false;
    ((ObjectHeader*)objectD.Address)->Alive = false;

    // --------------------------------------------------------
    // Retire region
    // --------------------------------------------------------

    allocation.Retire();

    Console.WriteLine(
        $"Region state before evacuation: {allocation.State}");

    Console.WriteLine(
        "AllocationRegion before:");

    PrintHeap(
        scanner,
        allocation);

    Console.WriteLine();
    Console.WriteLine(
        "GlobalHeap before:");

    PrintGlobalHeap(
        globalHeap);

    // --------------------------------------------------------
    // Evacuate
    // --------------------------------------------------------

    bool evacuated =
        evacuator.Evacuate(
            allocation);

    Console.WriteLine();

    Console.WriteLine(
        $"Evacuation completed: {evacuated}");

    Console.WriteLine(
        $"Region state after evacuation: {allocation.State}");

    Console.WriteLine();
    Console.WriteLine(
        "GlobalHeap after:");

    PrintGlobalHeap(
        globalHeap);

    // --------------------------------------------------------
    // Read updated handles
    // --------------------------------------------------------

    Handle movedA =
        handleTable.Read(
            handleA);

    Handle movedC =
        handleTable.Read(
            handleC);

    // --------------------------------------------------------
    // Print relocation
    // --------------------------------------------------------

    Console.WriteLine(
        $"A: 0x{addressA:X} -> 0x{movedA.Address:X}");

    Console.WriteLine(
        $"C: 0x{addressC:X} -> 0x{movedC.Address:X}");

    // --------------------------------------------------------
    // Verify data
    // --------------------------------------------------------

    VerifyObject(
        movedA,
        0xAA);

    VerifyObject(
        movedC,
        0xCC);

    // --------------------------------------------------------
    // Verify region state
    // --------------------------------------------------------

    if(!evacuated)
    {
        throw new InvalidOperationException(
            "Region evacuation failed.");
    }

    if(allocation.State != AllocationRegionState.Free)
    {
        throw new InvalidOperationException(
            "Region should be Free after evacuation.");
    }

    Console.WriteLine(
        "Region evacuation test passed.");
}

// ============================================================
// Global Heap Compaction Test
// ============================================================

static unsafe void TestGlobalHeapCompaction(
    HandleTable handleTable,
    GlobalHeap heap,
    GlobalHeapCompactor compactor)
{
    Console.WriteLine();
    Console.WriteLine(
        "=== Global Heap Compaction Test ===");

    Console.WriteLine();

    // --------------------------------------------------------
    // Allocate directly inside GlobalHeap
    // --------------------------------------------------------

    Handle objectA =
        AllocateGlobalObject(
            heap,
            handleTable,
            32);

    Handle objectB =
        AllocateGlobalObject(
            heap,
            handleTable,
            64);

    Handle objectC =
        AllocateGlobalObject(
            heap,
            handleTable,
            128);

    Handle objectD =
        AllocateGlobalObject(
            heap,
            handleTable,
            48);

    nint addressA =
        objectA.Address;

    nint addressB =
        objectB.Address;

    nint addressC =
        objectC.Address;

    nint addressD =
        objectD.Address;

    // --------------------------------------------------------
    // Initialize data
    // --------------------------------------------------------

    FillObject(objectA, 0xAA);
    FillObject(objectB, 0xBB);
    FillObject(objectC, 0xCC);
    FillObject(objectD, 0xDD);

    // --------------------------------------------------------
    // Mark B and D as dead
    // --------------------------------------------------------

    ((ObjectHeader*)addressB)->Alive = false;
    ((ObjectHeader*)addressD)->Alive = false;

    // --------------------------------------------------------
    // Save handle indexes
    // --------------------------------------------------------

    uint handleIndexA =
        ((ObjectHeader*)addressA)->HandleIndex;

    uint handleIndexC =
        ((ObjectHeader*)addressC)->HandleIndex;

    uint lengthA =
        ((ObjectHeader*)addressA)->Length;

    // --------------------------------------------------------
    // Before
    // --------------------------------------------------------

    Console.WriteLine(
        "Before compaction:");

    PrintGlobalHeap(
        heap);

    Console.WriteLine();

    // --------------------------------------------------------
    // Compact
    // --------------------------------------------------------

    nint freeStart =
        compactor.Compact(
            heap);

    // --------------------------------------------------------
    // After
    // --------------------------------------------------------

    Console.WriteLine(
        "After compaction:");

    PrintGlobalHeap(
        heap);

    Console.WriteLine();

    Console.WriteLine(
        $"Free start: 0x{freeStart:X}");

    // --------------------------------------------------------
    // Read updated handles
    // --------------------------------------------------------

    Handle movedA =
        handleTable.Read(
            handleIndexA);

    Handle movedC =
        handleTable.Read(
            handleIndexC);

    // --------------------------------------------------------
    // Verify A
    // --------------------------------------------------------

    if(movedA.Address != addressA)
    {
        throw new InvalidOperationException(
            "Object A should not have moved.");
    }

    // --------------------------------------------------------
    // Verify C
    // --------------------------------------------------------

    if(movedC.Address == addressC)
    {
        throw new InvalidOperationException(
            "Object C should have moved.");
    }

    nint expectedCAddress =
        addressA + (nint)lengthA;

    if(movedC.Address != expectedCAddress)
    {
        throw new InvalidOperationException(
            "Object C was moved to an unexpected address.");
    }

    // --------------------------------------------------------
    // Verify data
    // --------------------------------------------------------

    VerifyObject(
        movedA,
        0xAA);

    VerifyObject(
        movedC,
        0xCC);

    // --------------------------------------------------------
    // Print relocation
    // --------------------------------------------------------

    Console.WriteLine(
        $"A: 0x{addressA:X} -> 0x{movedA.Address:X}");

    Console.WriteLine(
        $"C: 0x{addressC:X} -> 0x{movedC.Address:X}");

    Console.WriteLine();

    Console.WriteLine(
        "Global heap compaction test passed.");
}

// ============================================================
// Allocate Object Directly in GlobalHeap
// ============================================================

static unsafe Handle AllocateGlobalObject(
    GlobalHeap heap,
    HandleTable handleTable,
    nuint dataSize)
{
    nuint totalSize =
        checked(
            (nuint)ObjectHeader.Size +
            dataSize);

    nint objectAddress =
        heap.Allocate(
            totalSize);

    if(objectAddress == 0)
    {
        throw new InvalidOperationException(
            "GlobalHeap is full.");
    }

    uint handleIndex =
        handleTable.Allocate(
            new Handle(objectAddress));

    ObjectHeader* header =
        (ObjectHeader*)objectAddress;

    *header =
        new ObjectHeader
        {
            Length =
                checked((uint)totalSize),

            HandleIndex =
                handleIndex,

            Alive =
                true,

            MetadataAddress =
                0
        };

    return new Handle(
        objectAddress);
}

// ============================================================
// Fill Object
// ============================================================

static unsafe void FillObject(
    Handle handle,
    byte value)
{
    ObjectHeader* header =
        (ObjectHeader*)handle.Address;

    byte* data =
        (byte*)header +
        ObjectHeader.Size;

    nuint dataSize =
        header->Length -
        (uint)ObjectHeader.Size;

    for(nuint i = 0; i < dataSize; i++)
    {
        data[i] = value;
    }
}

// ============================================================
// Verify Object
// ============================================================

static unsafe void VerifyObject(
    Handle handle,
    byte expected)
{
    ObjectHeader* header =
        (ObjectHeader*)handle.Address;

    byte* data =
        (byte*)header +
        ObjectHeader.Size;

    nuint dataSize =
        header->Length -
        (uint)ObjectHeader.Size;

    for(nuint i = 0; i < dataSize; i++)
    {
        if(data[i] != expected)
        {
            throw new InvalidOperationException(
                $"Object data corrupted at offset {i}.");
        }
    }
}

// ============================================================
// Print AllocationRegion
// ============================================================

static unsafe void PrintHeap(
    HeapScanner scanner,
    AllocationRegion allocation)
{
    scanner.Scan(
        allocation,
        (address, header) =>
        {
            Console.WriteLine(
                $"  0x{address:X} " +
                $"Length={header->Length}, " +
                $"HandleIndex={header->HandleIndex}, " +
                $"Alive={header->Alive}");
        });
}

// ============================================================
// Print GlobalHeap
// ============================================================

static unsafe void PrintGlobalHeap(
    GlobalHeap heap)
{
    var scanner =
        new GlobalHeapScanner();

    scanner.Scan(
        heap,
        (address, header) =>
        {
            Console.WriteLine(
                $"  0x{address:X} " +
                $"Length={header->Length}, " +
                $"HandleIndex={header->HandleIndex}, " +
                $"Alive={header->Alive}");
        });
}

static unsafe void TestConcurrentAccess(
    ObjectAllocator allocator,
    HandleTable handleTable)
{
    Console.WriteLine();
    Console.WriteLine(
        "=== Concurrent Access / GC Lock Test ===");

    Handle handle =
        allocator.Allocate(64);

    uint handleIndex =
        ((ObjectHeader*)handle.Address)->HandleIndex;

    HandleEntryRef entry =
        handleTable.GetEntry(handleIndex);

    Console.WriteLine(
        $"Initial: " +
        $"UsageCount={handle.UsageCount}, " +
        $"GCClocked={handle.GCLocked}, " +
        $"RequiresSync={handle.RequiresSync}");

    // --------------------------------------------------------
    // Scenario A:
    // Mutator owns the object.
    // --------------------------------------------------------

    if(!entry.TryAcquire())
    {
        throw new InvalidOperationException(
            "Mutator should be able to acquire object.");
    }

    Handle acquired =
        entry.Read();

    if(acquired.UsageCount != 1)
    {
        throw new InvalidOperationException(
            "UsageCount should be 1 after Acquire.");
    }

    Console.WriteLine(
        "Mutator acquired object.");

    // GC requests synchronization.
    entry.SetRequiresSync();

    Handle syncRequested =
        entry.Read();

    if(!syncRequested.RequiresSync)
    {
        throw new InvalidOperationException(
            "RequiresSync should be set.");
    }

    // GC must not be able to move the object
    // while mutator is using it.
    bool locked =
        entry.TryGCLock();

    if(locked)
    {
        throw new InvalidOperationException(
            "GC must not lock an object while UsageCount > 0.");
    }

    Console.WriteLine(
        "GC correctly deferred while object is in use.");

    // Mutator finishes.
    entry.Release();

    Handle released =
        entry.Read();

    if(released.UsageCount != 0)
    {
        throw new InvalidOperationException(
            "UsageCount should be 0 after Release.");
    }

    Console.WriteLine(
        "Mutator released object.");

    // Now GC can lock it.
    if(!entry.TryGCLock())
    {
        throw new InvalidOperationException(
            "GC should lock object after UsageCount reaches 0.");
    }

    Handle lockedHandle =
        entry.Read();

    if(!lockedHandle.GCLocked)
    {
        throw new InvalidOperationException(
            "GCClocked should be set.");
    }

    if(lockedHandle.UsageCount != 0)
    {
        throw new InvalidOperationException(
            "UsageCount must remain 0 while GC owns the object.");
    }

    Console.WriteLine(
        "GC successfully acquired exclusive ownership.");

    entry.GCUnlock();
    entry.ClearRequiresSync();

    // --------------------------------------------------------
    // Scenario B:
    // GC owns object before mutator attempts access.
    // --------------------------------------------------------

    entry.SetRequiresSync();

    if(!entry.TryGCLock())
    {
        throw new InvalidOperationException(
            "GC should be able to lock unused object.");
    }

    Handle gcOwned =
        entry.Read();

    if(!gcOwned.GCLocked)
    {
        throw new InvalidOperationException(
            "GCClocked should be set.");
    }

    Console.WriteLine(
        "GC acquired object before mutator.");

    // Mutator must now fail.
    bool acquiredDuringGc =
        entry.TryAcquire();

    if(acquiredDuringGc)
    {
        throw new InvalidOperationException(
            "Mutator must not acquire GCClocked object.");
    }

    Console.WriteLine(
        "Mutator correctly blocked while GC owns object.");

    // GC releases object.
    entry.GCUnlock();
    entry.ClearRequiresSync();

    // Mutator can access again.
    if(!entry.TryAcquire())
    {
        throw new InvalidOperationException(
            "Mutator should acquire object after GC unlock.");
    }

    Handle afterGc =
        entry.Read();

    if(afterGc.UsageCount != 1)
    {
        throw new InvalidOperationException(
            "UsageCount should be 1 after successful Acquire.");
    }

    entry.Release();

    Console.WriteLine(
        "Mutator successfully acquired object after GC unlock.");

    Console.WriteLine();
    Console.WriteLine(
        "Concurrent access / GC lock test passed.");
}