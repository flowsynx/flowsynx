using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsRegistriesList;

public class PluginsRegistriesListRequest : IRequest<PaginatedResult<PluginsRegistriesListResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
