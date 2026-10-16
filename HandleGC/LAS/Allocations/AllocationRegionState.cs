namespace HandleGC.LAS;

internal enum AllocationRegionState : uint
{
    Free,
    Active,
    Retired
}