using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Copy.Command;
using FlowSynx.Core.Features.Move.Command;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Move : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(DoMove);
    }

    public async Task<IResult> DoMove([FromBody] MoveRequest request,
        [FromServices] IMediator mediator, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Move(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}