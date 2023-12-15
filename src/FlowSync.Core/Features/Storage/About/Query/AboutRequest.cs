using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Storage.About.Query;

public class AboutRequest : IRequest<Result<AboutResponse>>
{
    public required string Path { get; set; }
    public bool? Full { get; set; } = false;
}