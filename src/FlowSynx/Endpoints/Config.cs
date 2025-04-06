using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.PluginConfig.Command.Add;
using FlowSynx.Application.Features.PluginConfig.Command.Update;
using FlowSynx.Application.Services;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Config : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", PluginsConfiguration)
            .WithName("PluginsConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));

        group.MapGet("/details/{id}", PluginConfigurationDetails)
            .WithName("PluginConfigurationDetails")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));

        group.MapPost("/add", AddPluginConfiguration)
            .WithName("AddPluginConfig")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));

        group.MapPost("/update/{id}", UpdatePluginConfiguration)
            .WithName("UpdatePluginConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));

        group.MapDelete("/delete/{id}", DeletePluginConfiguration)
            .WithName("DeletePluginConfiguration")
            .WithOpenApi()
            .RequireAuthorization(policy => policy.RequireRoleIgnoreCase("Admin", "Config"));
    }

    public async Task<IResult> PluginsConfiguration([FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.PluginsConfiguration(cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PluginConfigurationDetails(string id,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.PluginConfigurationDetails(id, cancellationToken);
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

    public async Task<IResult> UpdatePluginConfiguration(string id, HttpContext context,
        [FromServices] IMediator mediator, [FromServices] IJsonDeserializer jsonDeserializer,
        CancellationToken cancellationToken)
    {
        var jsonString = await new StreamReader(context.Request.Body).ReadToEndAsync(cancellationToken);
        var request = jsonDeserializer.Deserialize<UpdatePluginConfigModel>(jsonString);

        var updatePluginConfigRequest = new UpdatePluginConfigRequest { 
            Id = id, 
            Name = request.Name, 
            Type = request.Type, 
            Version = request.Version,
            Specifications = request.Specifications 
        };
        var result = await mediator.UpdatePluginConfiguration(updatePluginConfigRequest, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeletePluginConfiguration(string id,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.DeletePluginConfiguration(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}