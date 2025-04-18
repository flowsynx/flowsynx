﻿using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Query.List;

internal class PluginConfigListHandler : IRequestHandler<PluginConfigListRequest, Result<IEnumerable<PluginConfigListResponse>>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;

    public PluginConfigListHandler(ILogger<PluginConfigListHandler> logger, 
        IPluginConfigurationService pluginConfigurationService, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IEnumerable<PluginConfigListResponse>>> Handle(PluginConfigListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, "Access is denied. Authentication is required.");

            var pluginConfigs = await _pluginConfigurationService.All(_currentUserService.UserId, cancellationToken);
            var response = pluginConfigs.Select(config => new PluginConfigListResponse
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                ModifiedTime = config.LastModifiedOn
            });
            _logger.LogInformation("Plugin Config List is got successfully.");
            return await Result<IEnumerable<PluginConfigListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<PluginConfigListResponse>>.FailAsync(ex.ToString());
        }
    }
}