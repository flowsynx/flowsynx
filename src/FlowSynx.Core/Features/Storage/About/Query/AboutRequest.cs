using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.About.Query;

public class AboutRequest : IRequest<Result<AboutResponse>>
{
    public string Path { get; set; } = string.Empty;
    public bool? Full { get; set; } = false;
}