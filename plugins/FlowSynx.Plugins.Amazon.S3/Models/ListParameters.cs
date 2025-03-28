namespace FlowSynx.Plugins.Amazon.S3.Models;

internal class ListParameters
{
    public string Path { get; set; } = string.Empty;
    public string? Filter { get; set; }
    public bool? Recurse { get; set; } = false;
    public bool? CaseSensitive { get; set; } = false;
    public bool? IncludeMetadata { get; set; } = false;
    public int? MaxResults { get; set; } = int.MaxValue;
}