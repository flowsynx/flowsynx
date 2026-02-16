using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationsList;

public class WorkflowApplicationsListRequest : IAction<PaginatedResult<WorkflowApplicationsListResult>>
{
    public string Namespace { get; set; } = "default";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
