using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowDetails;

public class WorkflowDetailsRequest : IAction<Result<WorkflowDetailsResult>>
{
    public Guid Id { get; set; }
}
