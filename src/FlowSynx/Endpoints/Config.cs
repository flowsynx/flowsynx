using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Config.Command.Add;
using FlowSynx.Core.Features.Config.Command.Delete;
using FlowSynx.Core.Features.PluginConfig.Query.List;
using FlowSynx.Core.Services;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Config : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapPost("", PluginsConfiguration)
            .WithName("PluginsConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));

        group.MapPost("/details", PluginConfigurationDetails)
            .WithName("PluginConfigurationDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));

        group.MapPost("/add", AddPluginConfiguration)
            .WithName("AddPluginConfig")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));

        group.MapDelete("/delete", DeletePluginConfiguration)
            .WithName("DeletePluginConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));
    }

    public async Task<IResult> PluginsConfiguration(HttpContext context, 
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<PluginConfigListRequest>(jsonString);

        var result = await mediator.PluginsConfiguration(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PluginConfigurationDetails(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<PluginConfigDetailsRequest>(jsonString);

        var result = await mediator.PluginConfigurationDetails(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddPluginConfiguration(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<AddPluginConfigRequest>(jsonString);

        var result = await mediator.AddConfig(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeletePluginConfiguration(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<DeletePluginConfigRequest>(jsonString);

        var result = await mediator.DeleteConfig(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}