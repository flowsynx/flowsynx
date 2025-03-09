using MediatR;
using FlowSynx.Core.Wrapper;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Core.Features.Workflows.Command.Add;

public class AddWorkflowRequest : IRequest<Result<AddWorkflowResponse>>
{
    public required string Name { get; set; }
    public required JObject Template { get; set; }
}