using FlowSynx.Core.Wrapper;
using MediatR;

namespace FlowSynx.Core.Features.Workflows.Query.List;

public class WorkflowListRequest : IRequest<Result<IEnumerable<WorkflowListResponse>>>
{

}