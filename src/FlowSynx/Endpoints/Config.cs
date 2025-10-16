using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.PluginConfig.Command.AddPluginConfig;
using FlowSynx.Application.Features.PluginConfig.Command.UpdatePluginConfig;
using FlowSynx.Application.Serialization;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Config : EndpointGroupBase
{
    public override void Map(WebApplication app, string rateLimitPolicy)
    {
        var group = app.MapGroup(this)
                       .RequireRateLimiting(rateLimitPolicy);

        group.MapGet("", PluginsConfiguration)
            .WithName("PluginsConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "config"));

        group.MapPost("", AddPluginConfiguration)
            .WithName("AddPluginConfig")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "config"));

        group.MapGet("/{configId}", PluginConfigurationDetails)
            .WithName("PluginConfigurationDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "config"));

        group.MapPut("/{configId}", UpdatePluginConfiguration)
            .WithName("UpdatePluginConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "config"));

        group.MapDelete("/{configId}", DeletePluginConfiguration)
            .WithName("DeletePluginConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("admin", "config"));
    }

    public async Task<IResult> PluginsConfiguration(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await mediator.PluginsConfiguration(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddPluginConfiguration(HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<AddPluginConfigRequest>(jsonString);

        var result = await mediator.AddPluginConfiguration(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PluginConfigurationDetails(string configId,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.PluginConfigurationDetails(configId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> UpdatePluginConfiguration(string configId, HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<UpdatePluginConfigModel>(jsonString);

        var updatePluginConfigRequest = new UpdatePluginConfigRequest { 
            ConfigId = configId, 
            Name = request.Name, 
            Type = request.Type, 
            Version = request.Version,
            Specifications = request.Specifications 
        };
        var result = await mediator.UpdatePluginConfiguration(updatePluginConfigRequest, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeletePluginConfiguration(string configId,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.DeletePluginConfiguration(configId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
