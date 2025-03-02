//using FlowSynx.Core.Extensions;
//using FlowSynx.Core.Features.List.Query;
//using FlowSynx.Data.Extensions;
//using FlowSynx.Extensions;
//using MediatR;
//using Microsoft.AspNetCore.Mvc;

//namespace FlowSynx.Endpoints;

//public class List : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        app.MapGroup(this).MapPost(GetList);
//    }
    
//    public async Task<IResult> GetList([FromBody] ListRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.List(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result.Data.DataTableToList()) : Results.NotFound(result);
//    }
//}