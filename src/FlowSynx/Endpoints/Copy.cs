using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Copy.Command;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Copy : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(DoCopy);
    }

    public async Task<IResult> DoCopy([FromBody] CopyRequest request,
        [FromServices] IMediator mediator, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Copy(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}