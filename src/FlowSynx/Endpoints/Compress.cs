using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Compress.Command;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Compress : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(DoCompress);
    }

    public async Task<IResult> DoCompress([FromBody] CompressRequest request,
        [FromServices] IMediator mediator, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Compress(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        var resultData = result.Data;
        if (!string.IsNullOrEmpty(resultData.Md5))
            http.Response.Headers.Append("flowsynx-md5", resultData.Md5);

        return resultData.ContentType != null ?
            Results.Bytes(resultData.Content, resultData.ContentType) :
            Results.Bytes(resultData.Content);
    }
}