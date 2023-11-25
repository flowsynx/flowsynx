using FlowSync.Core.Extensions;
using FlowSync.Core.Features.Read.Query;
using FlowSync.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

public class Read : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(GetRead);
    }

    public async Task<IResult> GetRead([FromBody] ReadRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Read(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        return result.Data.Content == null ? Results.BadRequest() : Results.Stream(result.Data.Content);
    }
}