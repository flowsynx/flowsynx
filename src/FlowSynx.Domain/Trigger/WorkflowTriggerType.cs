namespace FlowSynx.Domain.Trigger;

public enum WorkflowTriggerType
{
    Manual = 0,
    TimeBased = 1,      // Cron or time-based scheduling
    EventBased = 2,     // Triggered by external events
    ApiBased = 3        // Triggered by API calls
}