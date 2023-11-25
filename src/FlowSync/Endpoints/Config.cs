using FlowSync.Core.Extensions;
using FlowSync.Core.Features.Config.Query.Details;
using FlowSync.Core.Features.Config.Query.List;
using FlowSync.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints;

public class Config : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(ConfigList)
            .MapGet(ConfigDetails, "details/{name}");
    }

    public async Task<IResult> ConfigList([AsParameters] ConfigListRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.ConfigList(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public async Task<IResult> ConfigDetails([AsParameters] ConfigDetailsRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.ConfigDetails(request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

}

//public static class Endpoint
//{
//    public static RouteGroupBuilder MapConfig(this RouteGroupBuilder group)
//    {
//        //group.MapGet("/", GetConfigList).WithName("ConfigList");
//        //group.MapGet("/details/{name}", GetConfigDetail).WithName("ConfigDetails");
//        //group.MapGet("/types", GetConfigList).WithName("Config"); //list based on plugin types
//        //group.MapGet("/create", GetConfigList).WithName("Config");
//        //group.MapGet("/delete", GetConfigList).WithName("Config");
//        //group.MapGet("/edit", GetConfigList).WithName("Config");
//        //group.MapGet("/file", GetConfigList).WithName("Config");
//        //group.MapGet("/touch", GetConfigList).WithName("Config");

//        return group;
//    }
//}