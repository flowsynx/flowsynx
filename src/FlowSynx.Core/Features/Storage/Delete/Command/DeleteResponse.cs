namespace FlowSynx.Core.Features.Storage.Delete.Command;

public class DeleteResponse
{
    public string? Id { get; set; }
    public string? Kind { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Path { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? MimeType { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}