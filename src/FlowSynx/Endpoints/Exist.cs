//using FlowSynx.Core.Extensions;
//using FlowSynx.Core.Features.Exist.Query;
//using FlowSynx.Core.Features.Write.Command;
//using FlowSynx.Extensions;
//using MediatR;
//using Microsoft.AspNetCore.Mvc;

//namespace FlowSynx.Endpoints;

//public class Exist : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        app.MapGroup(this).MapPost(CheckExistence);
//    }

//    public async Task<IResult> CheckExistence([FromBody] ExistRequest request,
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Exist(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }
//}