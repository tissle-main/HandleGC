namespace HandleGC.LAS.Allocations;

internal enum AllocationRegionState : uint
{
    Free,
    Active,
    Retired
}