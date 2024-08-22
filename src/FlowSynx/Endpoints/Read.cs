using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.About.Query;
using FlowSynx.Core.Features.Read.Query;
using FlowSynx.Extensions;
using FlowSynx.Plugin.Storage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Read : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(DoRead);
    }

    public async Task<IResult> DoRead([FromBody] ReadRequest request,
        [FromServices] IMediator mediator, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Read(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        var resultData = result.Data;
        if (resultData is not StorageRead read)
            return Results.Ok(result);

        if (!string.IsNullOrEmpty(read.Md5))
            http.Response.Headers.Append("flowsynx-md5", read.Md5);

        return read.ContentType != null ?
            Results.Stream(read.Stream, read.ContentType) :
            Results.Stream(read.Stream);
    }
}