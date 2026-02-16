using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.WorkflowApplications.Actions.ValidateWorkflowApplication;

public class ValidateWorkflowApplicationRequest : IRequest<Result<ValidationResponse>>
{
    public required object Json { get; set; }
}