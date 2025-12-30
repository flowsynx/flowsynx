using FlowSynx.Application.Services;
using FlowSynx.Domain.Primitives;
using FlowSynx.Localization;
using FlowSynx.PluginCore.Exceptions;
using System.Security.Claims;

namespace FlowSynx.Services;

/// <summary>
/// Provides safe access to the current HTTP user's identity information.
/// </summary>
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
            throw CreateFlowSynxException(ErrorCode.SecurityGetUserId, ex);
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
            throw CreateFlowSynxException(ErrorCode.SecurityGetUserId, ex);
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
            throw CreateFlowSynxException(ErrorCode.SecurityGetUserName, ex);
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
            throw CreateFlowSynxException(ErrorCode.SecurityCheckIsAuthenticated, ex);
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
            throw CreateFlowSynxException(ErrorCode.SecurityGetUserRoles, ex);
        }
    }

    /// <inheritdoc />
    public void ValidateAuthentication()
    {
        if (string.IsNullOrEmpty(UserId()))
        {
            throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired,
                FlowSynxResources.Authentication_Access_Denied);
        }
    }

    private FlowSynxException CreateFlowSynxException(ErrorCode errorCode, Exception exception)
    {
        var errorMessage = new ErrorMessage((int)errorCode, exception.Message);
        _logger.LogError(errorMessage.ToString());
        return new FlowSynxException(errorMessage);
    }
}
