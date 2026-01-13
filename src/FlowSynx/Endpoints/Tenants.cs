using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Features.Tenants.Actions.AddTenant;
using FlowSynx.Extensions;
using FlowSynx.Security;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class Tenants : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", TenantsList)
            .WithName("TenantsList")
            .RequirePermissions(Permissions.Admin, Permissions.Tenants);

        group.MapGet("/{id:guid}", TenantDetails)
            .WithName("TenantDetails")
            .RequirePermissions(Permissions.Admin, Permissions.Tenants);

        group.MapPost("", AddTenant)
            .WithName("AddTenant")
            .RequirePermissions(Permissions.Admin, Permissions.Tenants);

        //group.MapPut("/{id:guid}", UpdateTenant)
        //    .WithName("UpdateTenant")
        //    .RequirePermissions(Permissions.Admin, Permissions.Tenants);

        group.MapDelete("/{id:guid}", DeleteTenant)
            .WithName("DeleteTenant")
            .RequirePermissions(Permissions.Admin, Permissions.Tenants);
    }

    public static async Task<IResult> TenantsList(
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await dispatcher.Tenants(page, pageSize, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> TenantDetails(
        Guid id,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.TenantDetails(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    public static async Task<IResult> AddTenant(
        [FromBody] AddTenantRequest tenantRequest,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.AddTenant(tenantRequest, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }

    //public static async Task<IResult> UpdateTenant(
    //    Guid id,
    //    [FromBody] object json,
    //    [FromServices] IDispatcher dispatcher,
    //    CancellationToken cancellationToken)
    //{
    //    var result = await dispatcher.UpdateTenant(id, json, cancellationToken);
    //    return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    //}

    public static async Task<IResult> DeleteTenant(
        Guid id,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.DeleteTenant(id, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}
