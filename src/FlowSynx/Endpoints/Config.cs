using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Config.Command.Add;
using FlowSynx.Core.Features.Config.Command.Delete;
using FlowSynx.Core.Features.Config.Query.Details;
using FlowSynx.Core.Features.Config.Query.List;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Config : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(ConfigList)
            .MapGet(ConfigDetails, "/details/{name}")
            .MapPost(AddConfig, "/add")
            .MapDelete(DeleteConfig, "/delete");
    }

    public async Task<IResult> ConfigList([FromBody] ConfigListRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.ConfigList(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> ConfigDetails([AsParameters] ConfigDetailsRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.ConfigDetails(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> AddConfig([FromBody] AddConfigRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.AddConfig(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteConfig([FromBody] DeleteConfigRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteConfig(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}