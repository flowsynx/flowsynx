using FlowSync.Abstractions.Storage;

namespace FlowSync.Core.Features.Storage.Read.Query;

public class ReadResponse
{
    public StorageStream? Content { get; set; }
    public string? MimeType { get; set; }
}