using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.EnsureWorkflowPlugins;

public class EnsureWorkflowPluginsRequest : IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
}