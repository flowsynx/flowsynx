using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FlowSynx.Extensions;

public static class AuthorizationExtensions
{
    public static AuthorizationPolicyBuilder RequireRoleIgnoreCase(this AuthorizationPolicyBuilder builder, params string[] roles)
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