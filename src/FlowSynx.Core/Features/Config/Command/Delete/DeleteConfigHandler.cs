using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;
using FlowSynx.Configuration.Options;
using FlowSynx.Core.Features.List.Query;

namespace FlowSynx.Core.Features.Config.Command.Delete;

internal class DeleteConfigHandler : IRequestHandler<DeleteConfigRequest, Result<IEnumerable<DeleteConfigResponse>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IConfigurationManager _configurationManager;

    public DeleteConfigHandler(ILogger<ListHandler> logger, IConfigurationManager configurationManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public async Task<Result<IEnumerable<DeleteConfigResponse>>> Handle(DeleteConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var searchOptions = new ConfigurationSearchOptions
            {
                Include = request.Include,
                Exclude = request.Exclude,
                CaseSensitive = request.CaseSensitive,
                MinimumAge = request.MinimumAge,
                MaximumAge = request.MaximumAge,
            };

            var result = _configurationManager.Delete(searchOptions);
            var response = result.Select(c=> new DeleteConfigResponse
            {
                Id = c.Id
            });

            return await Result<IEnumerable<DeleteConfigResponse>>.SuccessAsync(response, Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<DeleteConfigResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}