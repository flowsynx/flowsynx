//using FlowSynx.Core.Extensions;
//using FlowSynx.Core.Features.Create.Command;
//using FlowSynx.Extensions;
//using MediatR;
//using Microsoft.AspNetCore.Mvc;

//namespace FlowSynx.Endpoints;

//public class Create : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        app.MapGroup(this).MapPost(DoCreate);
//    }

//    public async Task<IResult> DoCreate([FromBody] CreateRequest request,
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Create(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }
//}