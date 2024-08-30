using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Compress.Command;
using FlowSynx.Extensions;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Storage;
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
        if (resultData is not CompressResult compress)
            return Results.BadRequest(result);

        if (!string.IsNullOrEmpty(compress.Md5))
            http.Response.Headers.Append("flowsynx-md5", compress.Md5);

        return compress.ContentType != null ?
            Results.Stream(compress.Content, compress.ContentType) :
            Results.Stream(compress.Content);
    }
}