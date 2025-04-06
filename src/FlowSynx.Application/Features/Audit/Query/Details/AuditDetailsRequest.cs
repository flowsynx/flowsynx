using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Audit.Query.Details;

public class AuditDetailsRequest : IRequest<Result<AuditDetailsResponse>>
{
    public required string Id { get; set; }
}