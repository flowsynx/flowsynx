namespace FlowSynx.Application.Features.Plugins.Query.PluginsFullDetailsList;

public sealed class PluginsFullDetailsListResponse
{
    public string Type { get; init; } = default!;
    public string CategoryTitle { get; init; } = default!;
    public string? Description { get; init; }
    public IEnumerable<string> Versions { get; set; } = new List<string>();
    public string LatestVersion { get; set; } = string.Empty;
    public string Registry { get; init; } = default!;
}