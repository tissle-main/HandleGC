namespace HandleGC.LAS.Allocations;

internal enum AllocationRegionState : uint
{
    /// <summary>
    /// Region is free to use
    /// </summary>
    Free,

    /// <summary>
    /// Region in use
    /// </summary>
    Active,

    /// <summary>
    /// Region is full and gonna be freed by GC
    /// </summary>
    Retired
}