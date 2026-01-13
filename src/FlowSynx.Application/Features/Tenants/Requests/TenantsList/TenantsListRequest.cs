using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Tenants.Requests.TenantsList;

public class TenantsListRequest : IAction<PaginatedResult<TenantsListResult>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
