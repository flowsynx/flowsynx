using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Tenants.Actions.DeleteTenant;

public class DeleteTenantRequest : IAction<Result<DeleteTenantResult>>
{
    public Guid tenantId { get; set; }
}