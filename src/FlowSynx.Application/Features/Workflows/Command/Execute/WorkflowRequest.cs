using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowRequest : IRequest<Result<object?>>
{
    public required string WorkflowDefinition { get; set; }
}