using FlowSync.Abstractions.Entities;
using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Size.Query;

public class SizeRequest : IRequest<Result<SizeResponse>>
{
    public required string Path { get; set; }
    public string? Kind { get; set; } = FilterItemKind.FileAndDirectory.ToString();
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? MinSize { get; set; }
    public string? MaxSize { get; set; }
    public bool? FormatSize { get; set; } = true;
    public string? Sorting { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public bool? Recurse { get; set; } = false;
    public int? MaxResults { get; set; } = 10;
}