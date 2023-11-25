using FlowSync.Core.Extensions;
using FlowSync.Core.Features.Version.Query;
using FlowSync.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

public class Version : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetVersion);
    }

    public async Task<IResult> GetVersion([AsParameters] VersionRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Version(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}