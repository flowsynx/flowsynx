using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.PluginConfig.Query.PluginConfigList;

public class PluginConfigListRequest : IRequest<PaginatedResult<PluginConfigListResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
