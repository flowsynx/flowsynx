using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Workflow;

public class WorkflowRequest : IRequest<Result<object?>>
{
    public string WorkflowTemplate { get; set; }

    public WorkflowRequest(string workflowTemplate)
    {
        WorkflowTemplate = workflowTemplate;
    }
}