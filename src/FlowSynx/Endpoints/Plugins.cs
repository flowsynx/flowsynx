using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.Plugins.Command.InstallPlugin;
using FlowSynx.Application.Features.Plugins.Command.UninstallPlugin;
using FlowSynx.Application.Features.Plugins.Command.UpdatePlugin;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Plugins : EndpointGroupBase
{
    public override void Map(WebApplication app, string rateLimitPolicyName)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicyName);

        group.MapGet("", PluginsList)
            .WithName("PluginsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "plugins"));

        group.MapGet("/registries", RegistriesPluginsList)
            .WithName("RegistriesPluginsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "plugins"));

        group.MapGet("/{id}", PluginDetails)
            .WithName("PluginDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "plugins"));

        group.MapPost("", InstallPlugin)
            .WithName("InstallPlugin")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "plugins"));

        group.MapPut("", UpdatePlugin)
            .WithName("UpdatePlugin")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "plugins"));

        group.MapDelete("", UninstallPlugin)
            .WithName("UninstallPlugin")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "plugins"));
    }

    public async Task<IResult> PluginsList(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.PluginsList(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> RegistriesPluginsList(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.PluginsRegistriesList(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PluginDetails(string id, [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.PluginDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> InstallPlugin(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<InstallPluginRequest>(jsonString);

        var result = await mediator.InstallPlugin(request, cancellationToken);
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

    public async Task<IResult> UninstallPlugin(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<UninstallPluginRequest>(jsonString);

        var result = await mediator.UninstallPlugin(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
