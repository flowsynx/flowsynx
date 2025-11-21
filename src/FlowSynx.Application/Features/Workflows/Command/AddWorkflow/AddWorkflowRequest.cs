using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflow;

public class AddWorkflowRequest : IRequest<Result<AddWorkflowResponse>>
{
    public required string Definition { get; set; }
    public string? SchemaUrl { get; set; }
}
