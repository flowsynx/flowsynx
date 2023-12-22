using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.Version.Query;

public class VersionRequest : IRequest<Result<VersionResponse>>
{
    public bool? Check { get; set; } = false;
}