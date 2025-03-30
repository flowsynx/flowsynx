using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;

namespace FlowSynx.Application.Features.PluginConfig.Query.Details;

internal class PluginConfigDetailsHandler : IRequestHandler<PluginConfigDetailsRequest, Result<PluginConfigDetailsResponse>>
{
    private readonly ILogger<PluginConfigDetailsHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;

    public PluginConfigDetailsHandler(ILogger<PluginConfigDetailsHandler> logger, 
        IPluginConfigurationService pluginConfigurationService, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PluginConfigDetailsResponse>> Handle(PluginConfigDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, "Access is denied. Authentication is required.");

            var configId = Guid.Parse(request.Id);
            var pluginConfig = await _pluginConfigurationService.Get(_currentUserService.UserId, configId, cancellationToken);
            if (pluginConfig is null)
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, $"The config '{configId}' not found.");

            var response = new PluginConfigDetailsResponse
            {
                Id = pluginConfig.Id,
                Name = pluginConfig.Name,
                Type = pluginConfig.Type,
                Specifications = pluginConfig.Specifications,
            };
            _logger.LogInformation($"Plugin details for '{configId}' is executed successfully.");
            return await Result<PluginConfigDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<PluginConfigDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}