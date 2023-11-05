using FlowSync.Abstractions.Entities;
using MediatR;
using FlowSync.Core.Wrapper;
using FlowSync.Core.Enums;

namespace FlowSync.Core.Features.List;

public class ListRequest : IRequest<Result<IEnumerable<ListResponse>>>
{
    public required string Path { get; set; }
    public string? Kind { get; set; } = FilterItemKind.FileAndDirectory.ToString();
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? MinSize { get; set; }
    public string? MaxSize { get; set; }
    public string? Sorting { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public bool? Recurse { get; set; } = false;
    public int? MaxResults { get; set; } = 10;
    public string? Output { get; set; } = OutputType.Json.ToString();
}