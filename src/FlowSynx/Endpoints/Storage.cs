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
            .MapPost(About, "/about")
            .MapPost(List, "/list")
            .MapPost(Size, "/size")
            .MapPost(Read, "/read")
            .MapPost(Write, "/write")
            .MapDelete(Delete, "/delete")
            .MapDelete(DeleteFile, "/deleteFile")
            .MapPost(CheckExistence, "/exist")
            .MapPost(MakeDirectory, "/mkdir")
            .MapDelete(PurgeDirectory, "/purge")
            .MapPost(Copy, "/copy")
            .MapPost(Move, "/move")
            .MapPost(Check, "/check")
            .MapPost(Compress, "/compress");
    }

    public async Task<IResult> About([FromBody] AboutRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.About(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> List([FromBody] ListRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.List(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> Size([FromBody] SizeRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Size(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> Read([FromBody] ReadRequest request, 
        [FromServices] IMediator mediator, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Read(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        if (result.Data.Content == null) return Results.BadRequest();

        if (!string.IsNullOrEmpty(result.Data.Md5))
            http.Response.Headers.Add("flowsynx-md5", result.Data.Md5);

        return Results.Stream(result.Data.Content);

    }

    public async Task<IResult> Write([FromBody] WriteRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Write(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> Delete([FromBody] DeleteRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Delete(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> DeleteFile([FromBody] DeleteFileRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.DeleteFile(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> CheckExistence([FromBody] ExistRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Exist(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> MakeDirectory([FromBody] MakeDirectoryRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.MakeDirectory(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> PurgeDirectory([FromBody] PurgeDirectoryRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.PurgeDirectory(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> Copy([FromBody] CopyRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Copy(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> Move([FromBody] MoveRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Move(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> Check([FromBody] CheckRequest request, 
        [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Check(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> Compress([FromBody] CompressRequest request, 
        [FromServices] IMediator mediator, 
        HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Compress(request, cancellationToken);
        if (!result.Succeeded) return Results.NotFound(result);

        if (result.Data.Content == null) return Results.BadRequest();

        if (!string.IsNullOrEmpty(result.Data.Md5))
            http.Response.Headers.Add("flowsynx-md5", result.Data.Md5);

        return Results.Stream(result.Data.Content);
    }
}