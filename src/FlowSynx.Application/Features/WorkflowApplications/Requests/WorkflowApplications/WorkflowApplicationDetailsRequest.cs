using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationDetails;

public class WorkflowApplicationDetailsRequest : IAction<Result<WorkflowApplicationDetailsResult>>
{
    public Guid Id { get; set; }
}
