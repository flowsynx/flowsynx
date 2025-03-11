using MediatR;
using FlowSynx.Core.Wrapper;

namespace FlowSynx.Core.Features.Workflows.Command.Execute;

public class WorkflowRequest : IRequest<Result<object?>>
{
    public required string WorkflowDefinition { get; set; }
}