using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Transfer.Command;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Transfer : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(DoTransfer);
    }

    public async Task<IResult> DoTransfer([FromBody] TransferRequest request,
        [FromServices] IMediator mediator, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Transfer(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}