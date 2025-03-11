using MediatR;
using FlowSynx.Core.Wrapper;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Core.Features.Workflows.Command.Update;

public class UpdateWorkflowRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required JObject Definition { get; set; }
}