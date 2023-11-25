using FlowSync.Core.Extensions;
using FlowSync.Core.Features.Size.Query;
using FlowSync.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

public class Size : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(GetSize);
    }

    public async Task<IResult> GetSize([FromBody] SizeRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Size(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}