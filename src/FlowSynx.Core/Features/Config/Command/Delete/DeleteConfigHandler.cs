using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;
using FlowSynx.Core.Features.Storage.List.Query;

namespace FlowSynx.Core.Features.Config.Command.Delete;

internal class DeleteConfigHandler : IRequestHandler<DeleteConfigRequest, Result<DeleteConfigResponse>>
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

    public async Task<Result<DeleteConfigResponse>> Handle(DeleteConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _configurationManager.DeleteSetting(request.Name);
            return await Result<DeleteConfigResponse>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<DeleteConfigResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}