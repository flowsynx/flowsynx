namespace FlowSynx.Application.Features.Plugins.Query.PluginsRegistriesList;

public sealed class PluginsRegistriesListResponse
{
    public string Type { get; init; } = default!;
    public string CategoryTitle { get; init; } = default!;
    public string? Description { get; init; }
    public string? Version { get; init; }
    public string Registry { get; init; } = default!;
}