using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Plugins.Query.Details;
using FlowSynx.Core.Features.Plugins.Query.List;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Plugins : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetPlugins, "/{Namespace?}")
            .MapGet(GetPluginDetails, "/details/{id}");
    }

    public async Task<IResult> GetPlugins([AsParameters] PluginsListRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Plugins(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetPluginDetails([AsParameters] PluginDetailsRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.PluginDetails(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}