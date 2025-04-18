using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.PluginConfig.Command.Add;
using FlowSynx.Application.Features.Plugins.Command.Add;
using FlowSynx.Application.Features.Plugins.Command.Delete;
using FlowSynx.Application.Features.Plugins.Command.Update;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Plugins : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", PluginsList)
            .WithName("PluginsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Plugins"));

        group.MapGet("/details/{id}", PluginDetails)
            .WithName("PluginDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Plugins"));

        group.MapPost("/add", AddPlugin)
            .WithName("AddPlugin")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Plugins"));

        group.MapPost("/update", UpdatePlugin)
            .WithName("UpdatePlugin")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Plugins"));

        group.MapDelete("/delete", DeletePlugin)
            .WithName("DeletePlugin")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Plugins"));
    }

    public async Task<IResult> PluginsList([FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.PluginsList(cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PluginDetails(string id, [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.PluginDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddPlugin(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<AddPluginRequest>(jsonString);

        var result = await mediator.AddPlugin(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdatePlugin(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<UpdatePluginRequest>(jsonString);

        var result = await mediator.UpdatePlugin(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeletePlugin(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<DeletePluginRequest>(jsonString);

        var result = await mediator.DeletePlugin(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}