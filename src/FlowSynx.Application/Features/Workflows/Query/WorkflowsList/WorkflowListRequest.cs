using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowsList;

public class WorkflowListRequest : IRequest<Result<IEnumerable<WorkflowListResponse>>>
{

}