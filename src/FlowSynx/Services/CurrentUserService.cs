﻿using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using System.Security.Claims;

namespace FlowSynx.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly HttpContext? _httpContext;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContext = httpContextAccessor.HttpContext;
        _logger = logger;
    }

    public string UserId
    {
        get
        {
            try
            {
                var userId = _httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
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
                var userId = _httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
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
                if (identity == null)
                    return false;

                return identity.IsAuthenticated;
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
}