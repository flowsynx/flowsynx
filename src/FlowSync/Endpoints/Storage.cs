using FlowSync.Core.Extensions;
using FlowSync.Core.Features.Storage.About.Query;
using FlowSync.Core.Features.Storage.Delete.Command;
using FlowSync.Core.Features.Storage.List.Query;
using FlowSync.Core.Features.Storage.Read.Query;
using FlowSync.Core.Features.Storage.Size.Query;
using FlowSync.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

public class Storage : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(GetAbout, "/about")
            .MapPost(GetList, "/list")
            .MapPost(GetSize, "/size")
            .MapPost(GetRead, "/read")
            .MapPost(DoDelete, "/delete");
    }

    public async Task<IResult> GetAbout([FromBody] AboutRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.About(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetList([FromBody] ListRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.List(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetSize([FromBody] SizeRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Size(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoDelete([FromBody] DeleteRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Delete(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetRead([FromBody] ReadRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Read(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        return result.Data.Content == null ? Results.BadRequest() : Results.Stream(result.Data.Content);
    }
}