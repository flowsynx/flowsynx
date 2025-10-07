namespace FlowSynx.Domain.Trigger;

/// <summary>
/// Defines the different types of triggers that can start or schedule a workflow execution.
/// </summary>
public enum WorkflowTriggerType
{
    /// <summary>
    /// The workflow is triggered manually by a user.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// The workflow is triggered based on time or a schedule (e.g., CRON expression, interval, or specific date/time).
    /// </summary>
    Time = 1,

    /// <summary>
    /// The workflow is triggered in response to external system events (e.g., message queues, webhooks, or application signals).
    /// </summary>
    Event = 2,

    /// <summary>
    /// The workflow is triggered via a direct API call (e.g., HTTP request to an endpoint).
    /// </summary>
    Http = 3,

    /// <summary>
    /// The workflow is triggered when a file or folder changes (e.g., file creation, modification, or deletion).
    /// </summary>
    File = 4,

    /// <summary>
    /// The workflow is triggered based on database or data-related changes (e.g., new row inserted, value updated).
    /// </summary>
    DataBased = 5,
}