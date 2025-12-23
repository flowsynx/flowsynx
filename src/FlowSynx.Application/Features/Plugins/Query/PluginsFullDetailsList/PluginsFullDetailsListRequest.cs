using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsFullDetailsList;

public class PluginsFullDetailsListRequest : IRequest<PaginatedResult<PluginsFullDetailsListResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
