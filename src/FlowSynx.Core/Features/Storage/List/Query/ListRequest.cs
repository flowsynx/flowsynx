using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Features.Storage.List.Query;

public class ListRequest : IRequest<Result<IEnumerable<ListResponse>>>
{
    public required string Path { get; set; }
    public string? Kind { get; set; } = StorageFilterItemKind.FileAndDirectory.ToString();
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? MinSize { get; set; }
    public string? MaxSize { get; set; }
    public bool? Full { get; set; } = false;
    public string? Sorting { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public bool? Recurse { get; set; } = false;
    public bool? Hashing { get; set; } = false;
    public string? MaxResults { get; set; }
    public bool? IncludeMetadata { get; set; } = false;
}