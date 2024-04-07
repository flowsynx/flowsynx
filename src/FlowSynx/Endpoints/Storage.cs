using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.Storage.About.Query;
using FlowSynx.Core.Features.Storage.Check.Command;
using FlowSynx.Core.Features.Storage.Compress.Command;
using FlowSynx.Core.Features.Storage.Copy.Command;
using FlowSynx.Core.Features.Storage.Delete.Command;
using FlowSynx.Core.Features.Storage.DeleteFile.Command;
using FlowSynx.Core.Features.Storage.Exist.Query;
using FlowSynx.Core.Features.Storage.List.Query;
using FlowSynx.Core.Features.Storage.MakeDirectory.Command;
using FlowSynx.Core.Features.Storage.Move.Command;
using FlowSynx.Core.Features.Storage.PurgeDirectory.Command;
using FlowSynx.Core.Features.Storage.Read.Query;
using FlowSynx.Core.Features.Storage.Size.Query;
using FlowSynx.Core.Features.Storage.Write.Command;
using FlowSynx.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Storage : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(GetAbout, "/about")
            .MapPost(GetList, "/list")
            .MapPost(GetSize, "/size")
            .MapPost(DoRead, "/read")
            .MapPost(DoWrite, "/write")
            .MapDelete(DoDelete, "/delete")
            .MapDelete(DoDeleteFile, "/deleteFile")
            .MapPost(DoCheckExistence, "/exist")
            .MapPost(DoMakeDirectory, "/mkdir")
            .MapDelete(DoPurgeDirectory, "/purge")
            .MapPost(DoCopy, "/copy")
            .MapPost(DoMove, "/move")
            .MapPost(DoCheck, "/check")
            .MapPost(DoCompress, "/compress");
    }

    public async Task<IResult> GetAbout([FromBody] AboutRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.About(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetList([FromBody] ListRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.List(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> GetSize([FromBody] SizeRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Size(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoRead([FromBody] ReadRequest request, [FromServices] IMediator mediator, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Read(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        if (result.Data.Content == null) return Results.BadRequest();

        if (!string.IsNullOrEmpty(result.Data.Md5))
            http.Response.Headers.Add("flowsynx-md5", result.Data.Md5);

        return Results.Stream(result.Data.Content);

    }

    public async Task<IResult> DoWrite([FromBody] WriteRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Write(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoDelete([FromBody] DeleteRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Delete(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoDeleteFile([FromBody] DeleteFileRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteFile(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoCheckExistence([FromBody] ExistRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Exist(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoMakeDirectory([FromBody] MakeDirectoryRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.MakeDirectory(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoPurgeDirectory([FromBody] PurgeDirectoryRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.PurgeDirectory(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoCopy([FromBody] CopyRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Copy(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoMove([FromBody] MoveRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Move(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoCheck([FromBody] CheckRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Check(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DoCompress([FromBody] CompressRequest request, [FromServices] IMediator mediator, 
        HttpContext http, CancellationToken cancellationToken)
    {
        var result = mediator.Compress(request, cancellationToken).Result;
        if (!result.Succeeded) return Results.NotFound(result);

        if (result.Data.Content == null) return Results.BadRequest();

        if (!string.IsNullOrEmpty(result.Data.Md5))
            http.Response.Headers.Add("flowsynx-md5", result.Data.Md5);

        return Results.Stream(result.Data.Content);
    }
}