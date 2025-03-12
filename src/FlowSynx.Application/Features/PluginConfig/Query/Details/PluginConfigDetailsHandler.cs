using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Application.Services;

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
                throw new UnauthorizedAccessException("User is not authenticated.");

            var configId = Guid.Parse(request.Id);
            var pluginConfig = await _pluginConfigurationService.Get(_currentUserService.UserId, configId, cancellationToken);
            if (pluginConfig is null) 
                throw new Exception("The config not found");

            var response = new PluginConfigDetailsResponse
            {
                Id = pluginConfig.Id,
                Name = pluginConfig.Name,
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