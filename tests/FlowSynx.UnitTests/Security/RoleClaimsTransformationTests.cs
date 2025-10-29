using FlowSynx.Application.Configuration;
using FlowSynx.Security;
using System.Security.Claims;

namespace FlowSynx.UnitTests.Security;

/// <summary>
/// Verifies that <see cref="RoleClaimsTransformation"/> normalizes role claims across supported formats.
/// </summary>
public class RoleClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_WithJsonArrayClaim_AddsAllRolesFromArray()
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("roles", "[\"Admin\",\"User\"]"));
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleClaimsTransformation(new JwtAuthenticationsConfiguration());

        await sut.TransformAsync(principal);

        var roleValues = identity.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
        Assert.Contains("Admin", roleValues);
        Assert.Contains("User", roleValues);
        Assert.Equal(2, roleValues.Count);
    }

    [Fact]
    public async Task TransformAsync_WithCommaSeparatedClaim_SplitsAndTrimsEachRole()
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("roles", "Manager, Operator , Auditor"));
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleClaimsTransformation(new JwtAuthenticationsConfiguration());

        await sut.TransformAsync(principal);

        var roleValues = identity.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
        Assert.Contains("Manager", roleValues);
        Assert.Contains("Operator", roleValues);
        Assert.Contains("Auditor", roleValues);
        Assert.Equal(3, roleValues.Count);
    }

    [Fact]
    public async Task TransformAsync_WithInvalidJsonPayload_FallsBackToLiteralRole()
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("roles", "[not_valid_json"));
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleClaimsTransformation(new JwtAuthenticationsConfiguration());

        await sut.TransformAsync(principal);

        var roleValues = identity.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
        Assert.Contains("[not_valid_json", roleValues);
        Assert.Single(roleValues);
    }
}
