using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Features.Storage.Read.Query;

public class ReadResponse
{
    public Stream? Content { get; set; }
    public long Length => Content?.Length ?? 0;
    public string? Extension { get; set; }
    public string? MimeType { get; set; }
}