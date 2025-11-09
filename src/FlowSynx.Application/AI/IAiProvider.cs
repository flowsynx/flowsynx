namespace FlowSynx.Application.AI;

public interface IAiProvider
{
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Given a business goal and (optionally) a capabilities catalog, returns a JSON string
    /// that matches FlowSynx workflow schema. The handler validates it before use.
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(
        string goal,
        string? capabilitiesJson,
        CancellationToken cancellationToken);
}