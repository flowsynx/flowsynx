using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.About.Query;

public class AboutRequest : IRequest<Result<AboutResponse>>
{
    public required string Path { get; set; }
    public bool? Full { get; set; } = false;
}