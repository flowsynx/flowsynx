using FlowSynx.Core.Storage.Models;

namespace FlowSynx.Core.Features.Storage.Check.Command;

public class CheckResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public string State { get; set; } = string.Empty;
}