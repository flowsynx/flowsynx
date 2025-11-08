using FlowSynx.Application.Configuration.Security;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Linq;
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

    /// <summary>
    /// Extract roles from claims that may be expressed as JSON arrays, comma-delimited strings, or single values.
    /// </summary>
    /// <param name="identity">Identity that holds the raw claim set.</param>
    /// <param name="claimType">The claim type being evaluated for roles.</param>
    /// <param name="roles">Role accumulator that guards against duplicates.</param>
    private static void AddRolesFromClaim(ClaimsIdentity identity, string claimType, HashSet<string> roles)
    {
        foreach (var value in identity.FindAll(claimType)
                                      .Select(claim => claim.Value)
                                      .Where(claimValue => !string.IsNullOrWhiteSpace(claimValue)))
        {
            if (TryAddJsonArrayRoles(value, roles))
            {
                continue;
            }

            AddDelimitedOrSingleRole(value, roles);
        }
    }

    /// <summary>
    /// Parse a JSON array payload and append each non-empty entry as a role.
    /// </summary>
    /// <param name="claimValue">The raw claim value.</param>
    /// <param name="roles">Role accumulator.</param>
    /// <returns>True if roles were added from a JSON array payload; otherwise false.</returns>
    private static bool TryAddJsonArrayRoles(string claimValue, HashSet<string> roles)
    {
        if (!IsJsonArray(claimValue))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(claimValue);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                var role = element.GetString();
                if (!string.IsNullOrEmpty(role))
                {
                    roles.Add(role);
                }
            }

            return true;
        }
        catch
        {
            // Invalid JSON payloads fall back to normal parsing so behavior remains unchanged.
            return false;
        }
    }

    /// <summary>
    /// Quick check to avoid attempting JSON parsing when payload is a plain string.
    /// </summary>
    private static bool IsJsonArray(string value) =>
        value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal);

    /// <summary>
    /// Support comma-delimited list of roles or single role payloads.
    /// </summary>
    private static void AddDelimitedOrSingleRole(string claimValue, HashSet<string> roles)
    {
        if (claimValue.Contains(','))
        {
            foreach (var role in claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                roles.Add(role.Trim());
            }

            return;
        }

        roles.Add(claimValue);
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
