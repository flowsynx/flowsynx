namespace FlowSynx.Core.Features.Version.Query;

public class VersionResponse
{
    public required string FlowSynx { get; set; }
    public string? OSVersion { get; set; } = string.Empty;
    public string? OSArchitecture { get; set; } = string.Empty;
    public string? OSType { get; set; }
}