﻿using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Read.Query;
using FlowSynx.Extensions;
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
        //if (resultData is not StorageRead read)
        //    return Results.BadRequest(result);

        if (!string.IsNullOrEmpty(resultData.ContentHash))
            http.Response.Headers.Append("flowsynx-md5", resultData.ContentHash);

        return Results.Bytes(resultData.Content);
        //return read.ContentType != null ?
        //    Results.Bytes(read.Content, read.ContentType) :
        //    Results.Bytes(read.Content);
    }
}