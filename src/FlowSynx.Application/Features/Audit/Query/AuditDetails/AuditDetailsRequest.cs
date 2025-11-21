using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Audit.Query.AuditDetails;

public class AuditDetailsRequest : IRequest<Result<AuditDetailsResponse>>
{
    public required string AuditId { get; set; }
}