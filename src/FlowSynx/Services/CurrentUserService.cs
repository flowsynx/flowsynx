using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using System.Security.Claims;

namespace FlowSynx.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly HttpContext? _httpContext;
    private readonly ILogger<CurrentUserService> _logger;
    private readonly ILocalization _localization;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor, 
        ILogger<CurrentUserService> logger,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContext = httpContextAccessor.HttpContext;
        _logger = logger;
        _localization = localization;
    }

    public string UserId
    {
        get
        {
            try
            {
                var userId = _httpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                return userId;
            }
            catch (Exception ex)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.SecurityGetUserId, ex.Message);
                _logger.LogError(errorMessage.ToString());
                throw new FlowSynxException(errorMessage);
            }
        }
    }

    public string UserName
    {
        get
        {
            try
            {
                var userId = _httpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
                return userId;
            }
            catch (Exception ex)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.SecurityGetUserName, ex.Message);
                _logger.LogError(errorMessage.ToString());
                throw new FlowSynxException(errorMessage);
            }
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            try
            {
                var identity = _httpContext?.User.Identity;
                return identity is { IsAuthenticated: true };
            }
            catch (Exception ex)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.SecurityCheckIsAuthenticated, ex.Message);
                _logger.LogError(errorMessage.ToString());
                throw new FlowSynxException(errorMessage);
            }
        }
    }


    public List<string> Roles
    {
        get
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
                var errorMessage = new ErrorMessage((int)ErrorCode.SecurityGetUserRoles, ex.Message);
                _logger.LogError(errorMessage.ToString());
                throw new FlowSynxException(errorMessage);
            }
        }
    }

    public void ValidateAuthentication()
    {
        if (string.IsNullOrEmpty(UserId))
            throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired,
                _localization.Get("Authentication_Access_Denied"));
    }
}