using FlowSync.Core.Extensions;
using FlowSync.Core.Features.About.Query;
using FlowSync.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

public class About : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(GetAbout);
    }

    public async Task<IResult> GetAbout([FromBody] AboutRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.About(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}