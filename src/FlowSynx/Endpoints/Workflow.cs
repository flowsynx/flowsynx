using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Workflow;
using FlowSynx.Extensions;
using FlowSynx.IO.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Workflow : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(RunWorkflow);
    }
    
    public async Task<IResult> RunWorkflow(HttpContext context, 
        [FromServices] IMediator mediator, [FromServices] IDeserializer deserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var workflowTemplate = new WorkflowRequest(jsonString);

        var result = await mediator.Workflow(workflowTemplate, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}