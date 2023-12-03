using MediatR;
using FlowSync.Core.Common.Models;
using FlowSync.Abstractions.Storage;

namespace FlowSync.Core.Features.Storage.Delete.Command;

public class DeleteRequest : IRequest<Result<DeleteResponse>>
{
    public required string Path { get; set; }
    public string? Kind { get; set; } = StorageFilterItemKind.FileAndDirectory.ToString();
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
}