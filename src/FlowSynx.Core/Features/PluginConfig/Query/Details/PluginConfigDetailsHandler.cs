using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Core.Services;

namespace FlowSynx.Core.Features.PluginConfig.Query.Details;

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
                throw new UnauthorizedAccessException("User is not authenticated.");

            var pluginConfig = await _pluginConfigurationService.Get(_currentUserService.UserId, request.Name, cancellationToken);
            if (pluginConfig is null) 
                throw new Exception("The config not found");

            var response = new PluginConfigDetailsResponse
            {
                Name = request.Name,
                Type = pluginConfig.Type,
                Specifications = pluginConfig.Specifications,
            };
            _logger.LogInformation("Plugin details is executed successfully.");
            return await Result<PluginConfigDetailsResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<PluginConfigDetailsResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}