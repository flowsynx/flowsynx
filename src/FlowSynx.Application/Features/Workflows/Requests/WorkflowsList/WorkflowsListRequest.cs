using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowsList;

public class WorkflowsListRequest : IAction<PaginatedResult<WorkflowsListResult>>
{
    public string Namespace { get; set; } = "default";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
