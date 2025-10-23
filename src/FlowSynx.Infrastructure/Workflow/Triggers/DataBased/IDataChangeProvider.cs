namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Abstraction for extracting data change events from a concrete data source.
/// </summary>
public interface IDataChangeProvider
{
    /// <summary>
    /// Gets the provider key (e.g. POSTGRESQL, MYSQL, JSON) this provider supports.
    /// </summary>
    string ProviderKey { get; }

    /// <summary>
    /// Retrieves any new change events for the supplied trigger configuration.
    /// </summary>
    Task<IReadOnlyCollection<DataChangeEvent>> GetChangesAsync(
        DataTriggerConfiguration configuration,
        DataTriggerState state,
        CancellationToken cancellationToken);
}
