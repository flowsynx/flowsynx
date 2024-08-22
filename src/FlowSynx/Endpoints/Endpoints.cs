//using FlowSynx.Core.Extensions;
//using FlowSynx.Core.Features.About.Query;
//using FlowSynx.Core.Features.Check.Command;
//using FlowSynx.Core.Features.Compress.Command;
//using FlowSynx.Core.Features.Copy.Command;
//using FlowSynx.Core.Features.Create.Command;
//using FlowSynx.Core.Features.Delete.Command;
//using FlowSynx.Core.Features.DeleteFile.Command;
//using FlowSynx.Core.Features.Exist.Query;
//using FlowSynx.Core.Features.List.Query;
//using FlowSynx.Core.Features.Move.Command;
//using FlowSynx.Core.Features.PurgeDirectory.Command;
//using FlowSynx.Core.Features.Read.Query;
//using FlowSynx.Core.Features.Size.Query;
//using FlowSynx.Core.Features.Write.Command;
//using FlowSynx.Extensions;
//using FlowSynx.Plugin.Storage;
//using MediatR;
//using Microsoft.AspNetCore.Mvc;

//namespace FlowSynx.Endpoints;

//public class Endpoints : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        app
//            //.MapDelete(DeleteFile, "/deleteFile")
//            //.MapDelete(PurgeDirectory, "/purge")
//            //.MapPost(Copy, "/copy")
//            //.MapPost(Move, "/move")
//            //.MapPost(Check, "/check")
//            .MapPost(Compress, "/compress");
//    }
    
//    public async Task<IResult> DeleteFile([FromBody] DeleteFileRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.DeleteFile(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }

//    public async Task<IResult> PurgeDirectory([FromBody] PurgeDirectoryRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.PurgeDirectory(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }

//    public async Task<IResult> Copy([FromBody] CopyRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Copy(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }

//    public async Task<IResult> Move([FromBody] MoveRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Move(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }

//    public async Task<IResult> Check([FromBody] CheckRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Check(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }

//    public async Task<IResult> Compress([FromBody] CompressRequest request, 
//        [FromServices] IMediator mediator, 
//        HttpContext http, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Compress(request, cancellationToken);
//        if (!result.Succeeded) return Results.NotFound(result);

//        if (result.Data.Content == null) return Results.BadRequest();

//        if (!string.IsNullOrEmpty(result.Data.Md5))
//            http.Response.Headers.Append("flowsynx-md5", result.Data.Md5);

//        return Results.Stream(result.Data.Content);
//    }
//}