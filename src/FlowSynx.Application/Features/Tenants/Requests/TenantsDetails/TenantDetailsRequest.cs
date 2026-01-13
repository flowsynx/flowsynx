using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Tenants.Requests.TenantsDetails;

public class TenantDetailsRequest : IAction<Result<TenantDetailsResult>>
{
    public required Guid TenantId { get; set; }
}