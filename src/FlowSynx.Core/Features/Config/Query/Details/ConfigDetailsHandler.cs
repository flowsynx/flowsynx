using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;

namespace FlowSynx.Core.Features.Config.Query.Details;

internal class ConfigDetailsHandler : IRequestHandler<ConfigDetailsRequest, Result<ConfigDetailsResponse>>
{
    private readonly ILogger<ConfigDetailsHandler> _logger;
    private readonly IConfigurationManager _configurationManager;

    public ConfigDetailsHandler(ILogger<ConfigDetailsHandler> logger, IConfigurationManager configurationManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public async Task<Result<ConfigDetailsResponse>> Handle(ConfigDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = _configurationManager.GetSetting(request.Name);

            var response = new ConfigDetailsResponse
            {
                Id = result.Id,
                Name = result.Name,
                Type = result.Type,
                Specifications = result.Specifications
            };

            return await Result<ConfigDetailsResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ConfigDetailsResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}