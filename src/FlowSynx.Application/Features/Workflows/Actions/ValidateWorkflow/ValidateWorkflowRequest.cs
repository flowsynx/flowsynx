using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Workflows.Actions.ValidateWorkflow;

public class ValidateWorkflowRequest : IRequest<Result<ValidationResponse>>
{
    public required object Json { get; set; }
}