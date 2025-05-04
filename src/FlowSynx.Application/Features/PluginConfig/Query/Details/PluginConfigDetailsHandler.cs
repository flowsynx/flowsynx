using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.PluginConfig;

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
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var configId = Guid.Parse(request.Id);
            var pluginConfig = await _pluginConfigurationService.Get(_currentUserService.UserId, configId, cancellationToken);
            if (pluginConfig is null)
            {
                var message = string.Format(Resources.Feature_PluginConfig_DetailsNotFound, configId);
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, message);
            }

            var response = new PluginConfigDetailsResponse
            {
                Id = pluginConfig.Id,
                Name = pluginConfig.Name,
                Type = pluginConfig.Type,
                Version = pluginConfig.Version,
                Specifications = pluginConfig.Specifications,
            };
            _logger.LogInformation(string.Format(Resources.Feature_PluginConfig_DetailesRetrievedSuccessfully, configId));
            return await Result<PluginConfigDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<PluginConfigDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}