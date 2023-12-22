namespace FlowSynx.Core.Features.Storage.Version.Query;

public class VersionResponse
{
    public required string FlowSyncVersion { get; set; }
    public string? OSVersion { get; set; } = string.Empty;
    public string? OSArchitecture { get; set; } = string.Empty;
    public string? OSType { get; set; }
}