using MediatR;
using FlowSynx.Application.Wrapper;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Application.Features.Workflows.Command.Add;

public class AddWorkflowRequest : IRequest<Result<AddWorkflowResponse>>
{
    public required string Name { get; set; }
    public required JObject Template { get; set; }
}