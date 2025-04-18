﻿using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Command.Delete;

internal class DeletePluginConfigHandler : IRequestHandler<DeletePluginConfigRequest, Result<Unit>>
{
    private readonly ILogger<DeletePluginConfigHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;

    public DeletePluginConfigHandler(ILogger<DeletePluginConfigHandler> logger, ICurrentUserService currentUserService, 
        IPluginConfigurationService pluginConfigurationService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginConfigurationService = pluginConfigurationService;
    }

    public async Task<Result<Unit>> Handle(DeletePluginConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var configId = Guid.Parse(request.Id);
            var pluginConfiguration = await _pluginConfigurationService.Get(_currentUserService.UserId, configId, cancellationToken);
            if (pluginConfiguration == null)
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, $"The config '{configId}' not found");

            var deleteResult = await _pluginConfigurationService.Delete(pluginConfiguration, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(new List<string> { ex.ToString() });
        }
    }
}