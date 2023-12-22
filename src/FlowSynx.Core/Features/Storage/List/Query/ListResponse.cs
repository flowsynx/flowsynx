namespace FlowSynx.Core.Features.Storage.List.Query;

public class ListResponse
{
    public string? Id { get; set; }
    public string? Kind { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Path { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? MimeType { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}