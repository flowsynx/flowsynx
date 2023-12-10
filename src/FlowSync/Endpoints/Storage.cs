﻿using FlowSync.Core.Extensions;
using FlowSync.Core.Features.Storage.About.Query;
using FlowSync.Core.Features.Storage.Copy.Command;
using FlowSync.Core.Features.Storage.Delete.Command;
using FlowSync.Core.Features.Storage.DeleteFile.Command;
using FlowSync.Core.Features.Storage.List.Query;
using FlowSync.Core.Features.Storage.MakeDirectory.Command;
using FlowSync.Core.Features.Storage.Move.Command;
using FlowSync.Core.Features.Storage.PurgeDirectory.Command;
using FlowSync.Core.Features.Storage.Read.Query;
using FlowSync.Core.Features.Storage.Size.Query;
using FlowSync.Core.Features.Storage.Write.Command;
using FlowSync.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

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
            .MapPost(DoDelete, "/delete")
            .MapPost(DoDeleteFile, "/deleteFile")
            .MapPost(DoMakeDirectory, "/mkdir")
            .MapPost(DoPurgeDirectory, "/purge")
            .MapPost(DoCopy, "/copy")
            .MapPost(DoMove, "/move");
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

    public async Task<IResult> DoRead([FromBody] ReadRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Read(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        return result.Data.Content == null ? Results.BadRequest() : Results.Stream(result.Data.Content);
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
}