namespace HandleGC;

public enum AllocationRegionState
{
    Active,
    Retired,
    Compacting,
    Free
}