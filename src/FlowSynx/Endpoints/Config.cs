using FlowSynx.Core.Extensions;
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

            //.MapPost(ConfigDetails, "/details")
            //.MapPost(AddConfig, "/add")
            //.MapDelete(DeleteConfig, "/delete");
    }

    public async Task<IResult> PluginsConfiguration([FromServices] IMediator mediator, 
        [FromBody] PluginConfigListRequest request, ICurrentUserService currentUser, 
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var result = await mediator.PluginsConfiguration(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PluginConfigurationDetails([FromBody] PluginConfigDetailsRequest request,
        [FromServices] IMediator mediator, ICurrentUserService currentUser, 
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var result = await mediator.PluginConfigurationDetails(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    //public async Task<IResult> AddConfig([FromBody] AddConfigRequest request,
    //    [FromServices] IMediator mediator, CancellationToken cancellationToken)
    //{
    //    var result = await mediator.AddConfig(request, cancellationToken);
    //    return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    //}

    //public async Task<IResult> DeleteConfig([FromBody] DeleteConfigRequest request,
    //    [FromServices] IMediator mediator, CancellationToken cancellationToken)
    //{
    //    var result = await mediator.DeleteConfig(request, cancellationToken);
    //    return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    //}
}