namespace FlowSync.Core.Features.Read.Query;

public class ReadResponse
{
    public FileStream? Content { get; set; }
    public string? MimeType { get; set; }
}