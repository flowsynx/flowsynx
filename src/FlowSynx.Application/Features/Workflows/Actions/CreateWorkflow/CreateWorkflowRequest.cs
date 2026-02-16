using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Workflows.Actions.CreateWorkflow;

public class CreateWorkflowRequest : IRequest<Result<CreateWorkflowResult>>
{
    public required object Json { get; set; }
}