using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Query.List;

public class WorkflowListRequest : IRequest<Result<IEnumerable<WorkflowListResponse>>>
{

}