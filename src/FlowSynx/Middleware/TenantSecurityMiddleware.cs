//using FlowSynx.Application;
//using FlowSynx.Application.Services;
//using FlowSynx.Domain.Tenants;
//using FlowSynx.Extensions;
//using FlowSynx.Security;
//using Serilog.Context;

//namespace FlowSynx.Middleware;

//public class TenantSecurityMiddleware
//{
//    private readonly RequestDelegate _next;

//    public TenantSecurityMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    public async Task InvokeAsync(
//        HttpContext context, 
//        ITenantService tenantService, 
//        ITenantRepository tenantRepository)
//    {
//        var tenantId = tenantService.GetCurrentTenantId();

//        if (tenantId == null)
//        {
//            context.Response.StatusCode = StatusCodes.Status400BadRequest;
//            await context.Response.WriteAsync("Tenant not found");
//            return;
//        }

//        var tenant = await tenantRepository.GetByIdAsync(tenantId, CancellationToken.None);
//        var factory = context.RequestServices.GetRequiredService<ITenantAuthenticationFactory>();
//        var authenticator = factory.GetAuthenticator(tenant.Configuration.Security.Authentication.Mode);

//        try
//        {
//            await authenticator.AuthenticateAsync(context, tenant);
//            await _next(context);
//        }
//        catch (UnauthorizedAccessException ex)
//        {
//            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//            await context.Response.WriteAsync(ex.Message);
//        }
//    }
//}