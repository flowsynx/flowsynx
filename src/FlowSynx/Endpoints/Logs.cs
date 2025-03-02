//using FlowSynx.Core.Extensions;
//using FlowSynx.Core.Features.Logs.Query.List;
//using FlowSynx.Extensions;
//using MediatR;
//using Microsoft.AspNetCore.Mvc;

//namespace FlowSynx.Endpoints;

//public class Logs : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        app.MapGroup(this)
//            .MapPost(GetLogs);
//    }

//    public async Task<IResult> GetLogs([FromBody] LogsListRequest listRequest, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Logs(listRequest, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }
//}