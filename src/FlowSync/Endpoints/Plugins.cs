using FlowSync.Core.Extensions;
using FlowSync.Core.Features.Plugins.Query;
using FlowSync.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

public class Plugins : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetPlugins, "/{Namespace?}");
    }

    public async Task<IResult> GetPlugins([AsParameters] PluginRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Plugins(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}