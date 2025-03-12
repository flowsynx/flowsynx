using MediatR;
using FlowSynx.Application.Wrapper;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Application.Features.Workflows.Command.Update;

public class UpdateWorkflowRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required JObject Definition { get; set; }
}