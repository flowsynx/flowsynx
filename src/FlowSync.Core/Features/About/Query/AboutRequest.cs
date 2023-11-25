using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.About.Query;

public class AboutRequest : IRequest<Result<AboutResponse>>
{
    public required string Path { get; set; }
    public bool? FormatSize { get; set; } = true;
}