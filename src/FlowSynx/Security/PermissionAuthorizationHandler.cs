using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FlowSynx.Security;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
            return Task.CompletedTask;

        // Admin override
        if (context.User.HasClaim(CustomClaimTypes.Permissions, Permissions.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(CustomClaimTypes.Permissions, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}