using MediatR;
using FlowSynx.Core.Wrapper;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Core.Features.Workflows.Command.Update;

public class UpdateWorkflowRequest : IRequest<Result<Unit>>
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required JObject Template { get; set; }
}