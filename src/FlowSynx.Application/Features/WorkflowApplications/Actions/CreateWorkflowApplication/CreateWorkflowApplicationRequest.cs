using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.WorkflowApplications.Actions.CreateWorkflowApplication;

public class CreateWorkflowApplicationRequest : IRequest<Result<CreateWorkflowApplicationResult>>
{
    public required object Json { get; set; }
}