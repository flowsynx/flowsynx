using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.PluginConfig.Query.Details;
using FlowSynx.Application.Features.Workflows.Command.Add;
using FlowSynx.Application.Features.Workflows.Command.Delete;
using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Features.Workflows.Command.Update;
using FlowSynx.Application.Features.Workflows.Query.Details;
using FlowSynx.Application.Features.Workflows.Query.List;
using FlowSynx.Application.Services;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Workflows : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapPost("", WorkflowsList)
            .WithName("WorkflowsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/details", WorkflowDetails)
            .WithName("WorkflowDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/add", AddWorkflow)
            .WithName("AddWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/update", UpdateWorkflow)
            .WithName("UpdateWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapDelete("/delete", DeleteWorkflow)
            .WithName("DeleteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/execute/{id}", ExecuteWorkflow)
            .WithName("ExecuteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));
    }

    public async Task<IResult> WorkflowsList(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<WorkflowListRequest>(jsonString);

        var result = await mediator.Workflows(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> WorkflowDetails(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<WorkflowDetailsRequest>(jsonString);

        var result = await mediator.WorkflowDetails(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddWorkflow(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<AddWorkflowRequest>(jsonString);

        var result = await mediator.AddWorkflow(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdateWorkflow(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<UpdateWorkflowRequest>(jsonString);

        var result = await mediator.UpdateWorkflow(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteWorkflow(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<DeleteWorkflowRequest>(jsonString);

        var result = await mediator.DeleteWorkflow(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> ExecuteWorkflow(Guid id, [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        //var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var result = await mediator.ExecuteWorkflow(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}





//public class Workflows : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        app.MapGroup(this).MapPost(RunWorkflow);
//    }

//    public async Task<IResult> RunWorkflow(HttpContext context,
//        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer deserializer,
//        CancellationToken cancellationToken)
//    {
//        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
//        var workflowTemplate = new WorkflowRequest(jsonString);

//        var result = await mediator.Workflow(workflowTemplate, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }
//}