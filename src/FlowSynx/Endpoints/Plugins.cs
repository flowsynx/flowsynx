using FlowSynx.Application.Extensions;
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
}