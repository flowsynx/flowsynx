using FlowSynx.Core.Extensions;
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

        if (!string.IsNullOrEmpty((string)resultData.Rows[0]["ContentHash"]))
            http.Response.Headers.Append("flowsynx-md5", (string)resultData.Rows[0]["ContentHash"]);

        return Results.Bytes((byte[])resultData.Rows[0]["Content"]);
        //return read.ContentType != null ?
        //    Results.Bytes(read.Content, read.ContentType) :
        //    Results.Bytes(read.Content);
    }
}