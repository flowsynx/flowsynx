using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.PluginConfig.Query.List;
using FlowSynx.Core.Features.Plugins.Query.Details;
using FlowSynx.Core.Features.Plugins.Query.List;
using FlowSynx.Core.Services;
using FlowSynx.Domain.Entities.PluginConfig;
using FlowSynx.Extensions;
using FlowSynx.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Plugins : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapPost("", PluginsList)
            .WithName("PluginsList")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Plugins"));

        group.MapPost("/details", PluginDetails)
            .WithName("PluginDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Plugins"));
    }

    public async Task<IResult> PluginsList(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<PluginsListRequest>(jsonString);

        var result = await mediator.PluginsList(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PluginDetails(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<PluginDetailsRequest>(jsonString);

        var result = await mediator.PluginDetails(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}