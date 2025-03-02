//using FlowSynx.Core.Extensions;
//using FlowSynx.Core.Features.Connectors.Query.Details;
//using FlowSynx.Core.Features.Connectors.Query.List;
//using FlowSynx.Extensions;
//using MediatR;
//using Microsoft.AspNetCore.Mvc;

//namespace FlowSynx.Endpoints;

//public class Connectors : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        app.MapGroup(this)
//            .MapPost(GetConnectors)
//            .MapPost(GetConnectorDetails, "/details");
//    }

//    public async Task<IResult> GetConnectors([FromBody] ConnectorListRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.Connectors(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }

//    public async Task<IResult> GetConnectorDetails([FromBody] ConnectorDetailsRequest request, 
//        [FromServices] IMediator mediator, CancellationToken cancellationToken)
//    {
//        var result = await mediator.ConnectorDetails(request, cancellationToken);
//        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
//    }
//}