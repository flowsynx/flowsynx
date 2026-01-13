using FlowSynx.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FlowSynx.Extensions;

public static class AuthorizationExtensions
{
    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in Permissions.All)
        {
            options.AddPolicy(permission, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(CustomClaimTypes.TenantId);
                policy.Requirements.Add(new PermissionRequirement(permission));
            });
        }
    }

    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder builder,
        string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequirePermissions(permission);
    }

    public static TBuilder RequirePermissions<TBuilder>(
        this TBuilder builder,
        params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        if (permissions is null || permissions.Length == 0)
        {
            return builder;
        }

        builder.Add(endpointBuilder =>
        {
            foreach (var permission in permissions)
            {
                if (string.IsNullOrWhiteSpace(permission))
                {
                    continue;
                }

                endpointBuilder.Metadata.Add(new AuthorizeAttribute
                {
                    Policy = permission
                });
            }
        });

        return builder;
    }

    public static AuthorizationPolicyBuilder RequireRoleIgnoreCase(
        this AuthorizationPolicyBuilder builder,
        params string[] roles)
    {
        return builder.RequireAssertion(context =>
        {
            var userRoles = context.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value.ToLower())
                .ToHashSet();

            return roles.Any(role => userRoles.Contains(role.ToLower()));
        });
    }
}