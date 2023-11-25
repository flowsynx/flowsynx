using FlowSync.Abstractions.Entities;
using FlowSync.Core.Enums;

namespace FlowSync.Core.Features.List;

public class ListResponse
{
    public string? Kind { get; set; }
    public string? Name { get; set; } = string.Empty;
    public long? Size { get; set; } = 0;
    public DateTimeOffset? DateCreated { get; set; }
}