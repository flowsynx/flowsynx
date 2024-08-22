using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Write.Command;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Write : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(DoWrite);
    }

    public async Task<IResult> DoWrite([FromBody] WriteRequest request,
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Write(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}