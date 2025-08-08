using FlowSynx.Application.Configuration;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace FlowSynx.Security;

public class RoleClaimsTransformation : IClaimsTransformation
{
    private readonly JwtAuthenticationsConfiguration _jwtAuthenticationsConfiguration;

    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="clientId">Optional client ID to look for roles under resource_access.{clientId}.roles (Keycloak style)</param>
    /// <param name="roleClaimNames">Optional claim names to check for roles (default: "roles", "role", "groups")</param>
    public RoleClaimsTransformation(JwtAuthenticationsConfiguration jwtAuthenticationsConfiguration)
    {
        _jwtAuthenticationsConfiguration = jwtAuthenticationsConfiguration;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is ClaimsIdentity identity)
        {
            var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Scan flat claims with configured role claim names
            foreach (var claimName in _jwtAuthenticationsConfiguration.RoleClaimNames)
            {
                AddRolesFromClaim(identity, claimName, roles);
            }

            // 2) Check nested common Keycloak claims if clientId provided
            AddRolesFromNestedClaim(identity, "realm_access", "roles", roles);

            if (!string.IsNullOrEmpty(_jwtAuthenticationsConfiguration.Audience))
            {
                AddRolesFromNestedClaim(identity, "resource_access", _jwtAuthenticationsConfiguration.Audience, roles);
            }

            // Add unique roles as ClaimTypes.Role claims if not already present
            foreach (var role in roles)
            {
                if (!identity.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        return Task.FromResult(principal);
    }

    private void AddRolesFromClaim(ClaimsIdentity identity, string claimType, HashSet<string> roles)
    {
        var claims = identity.FindAll(claimType);
        foreach (var claim in claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
                continue;

            // Try parse as JSON array (some providers encode roles as JSON string arrays)
            if (claim.Value.StartsWith("[") && claim.Value.EndsWith("]"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(claim.Value);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in doc.RootElement.EnumerateArray())
                        {
                            var role = element.GetString();
                            if (!string.IsNullOrEmpty(role))
                                roles.Add(role);
                        }
                        continue;
                    }
                }
                catch
                {
                    // Not JSON array - ignore parsing error
                }
            }

            // Otherwise, treat as single role string (comma-separated list?)
            if (claim.Value.Contains(","))
            {
                foreach (var role in claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    roles.Add(role.Trim());
                }
            }
            else
            {
                roles.Add(claim.Value);
            }
        }
    }

    private void AddRolesFromNestedClaim(ClaimsIdentity identity, string parentClaimType, string childClaimType, HashSet<string> roles)
    {
        var parentClaim = identity.FindFirst(parentClaimType);
        if (parentClaim == null || string.IsNullOrWhiteSpace(parentClaim.Value))
            return;

        try
        {
            using var doc = JsonDocument.Parse(parentClaim.Value);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return;

            if (!doc.RootElement.TryGetProperty(childClaimType, out JsonElement childElement))
                return;

            if (childElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var roleElement in childElement.EnumerateArray())
                {
                    var role = roleElement.GetString();
                    if (!string.IsNullOrEmpty(role))
                        roles.Add(role);
                }
            }
            else if (childElement.ValueKind == JsonValueKind.String)
            {
                var role = childElement.GetString();
                if (!string.IsNullOrEmpty(role))
                    roles.Add(role);
            }
        }
        catch
        {
            // Ignore invalid JSON or missing props
        }
    }
}