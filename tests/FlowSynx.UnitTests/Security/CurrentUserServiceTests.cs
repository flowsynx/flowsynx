using FlowSynx.Application.Localizations;
using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace FlowSynx.UnitTests.Security;

/// <summary>
/// Exercises the method-based contract exposed by <see cref="CurrentUserService"/>.
/// </summary>
public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_ReturnsIdentifier_WhenClaimIsPresent()
    {
        var accessor = BuildAccessor(new Claim(ClaimTypes.NameIdentifier, "user-123"));
        var sut = CreateService(accessor);

        var result = sut.UserId();

        Assert.Equal("user-123", result);
    }

    [Fact]
    public void Roles_ReturnsEmptyCollection_WhenPrincipalMissing()
    {
        var accessor = new HttpContextAccessor();
        var sut = CreateService(accessor);

        var result = sut.Roles();

        Assert.Empty(result);
    }

    [Fact]
    public void ValidateAuthentication_ThrowsFlowSynxException_WhenUserIdMissing()
    {
        var accessor = BuildAccessor();
        var sut = CreateService(accessor);

        var exception = Assert.Throws<FlowSynxException>(sut.ValidateAuthentication);

        Assert.Equal((int)ErrorCode.SecurityAuthenticationIsRequired, exception.ErrorCode);
        Assert.Contains("Authentication_Access_Denied", exception.Message);
    }

    private static CurrentUserService CreateService(IHttpContextAccessor accessor) =>
        new(accessor, NullLogger<CurrentUserService>.Instance, new TestLocalization());

    private static IHttpContextAccessor BuildAccessor(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext();

        if (claims.Length > 0)
        {
            var identity = new ClaimsIdentity("TestAuth");
            identity.AddClaims(claims);
            httpContext.User = new ClaimsPrincipal(identity);
        }

        return new HttpContextAccessor { HttpContext = httpContext };
    }

    private sealed class TestLocalization : ILocalization
    {
        public string Get(string key) => key;

        public string Get(string key, params object[] args) =>
            $"{key}:{string.Join(',', args)}";
    }
}
