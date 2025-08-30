using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionTasks;

public class WorkflowExecutionTasksRequest : IRequest<Result<IEnumerable<WorkflowExecutionTasksResponse>>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
}