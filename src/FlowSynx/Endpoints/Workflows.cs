using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Workflows.Command.Delete;
using FlowSynx.Application.Features.Workflows.Command.Update;
using FlowSynx.Application.Features.Workflows.Query.Details;
using FlowSynx.Application.Features.Workflows.Query.List;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Workflows : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", WorkflowsList)
            .WithName("WorkflowsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/details/{id}", WorkflowDetails)
            .WithName("WorkflowDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/add", AddWorkflow)
            .WithName("AddWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapPost("/update/{id}", UpdateWorkflow)
            .WithName("UpdateWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapDelete("/delete/{id}", DeleteWorkflow)
            .WithName("DeleteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));

        group.MapGet("/execute/{id}", ExecuteWorkflow)
            .WithName("ExecuteWorkflow")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Workflows"));
    }

    public async Task<IResult> WorkflowsList([FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Workflows(cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> WorkflowDetails(string id, [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.WorkflowDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddWorkflow(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var result = await mediator.AddWorkflow(jsonString, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdateWorkflow(string id, HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var result = await mediator.UpdateWorkflow(id, jsonString, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteWorkflow(string id, [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteWorkflow(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> ExecuteWorkflow(Guid id, [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.ExecuteWorkflow(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}