using FlowSynx.Application.Core.Services;
using FlowSynx.Exceptions;
using System.Security.Claims;

namespace FlowSynx.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly HttpContext? _httpContext;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor, 
        ILogger<CurrentUserService> logger)
    {
        _httpContext = httpContextAccessor.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string TenantId()
    {
        try
        {
            return _httpContext?.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant ID.");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public string UserId()
    {
        try
        {
            return _httpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user ID.");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public string UserName()
    {
        try
        {
            return _httpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user name.");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated()
    {
        try
        {
            var identity = _httpContext?.User.Identity;
            return identity is { IsAuthenticated: true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status.");
            return false;
        }
    }

    /// <inheritdoc />
    public List<string> Roles()
    {
        try
        {
            var user = _httpContext?.User;
            if (user == null)
                return new List<string>();

            var roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user roles.");
            return new List<string>();
        }
    }

    /// <inheritdoc />
    public void ValidateAuthentication()
    {
        if (string.IsNullOrEmpty(UserId()))
        {
            throw new AuthenticationRequiredException();
        }
    }
}
