using FlowSynx.Core.Services;
using System.Security.Claims;

namespace FlowSynx.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly HttpContext? _httpContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContext = httpContextAccessor.HttpContext;
    }

    public string UserId
    {
        get
        {
            var userId = _httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            return userId;
        }
    }

    public string UserName
    {
        get
        {
            var userId = _httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            return userId;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            var identity = _httpContext?.User.Identity;
            if (identity == null) 
                return false;

            return identity.IsAuthenticated;
        }
    }


    public List<string> Roles
    {
        get
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
    }
}