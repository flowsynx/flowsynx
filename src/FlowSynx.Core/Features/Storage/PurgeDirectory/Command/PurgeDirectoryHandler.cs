using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.PurgeDirectory.Command;

internal class PurgeDirectoryHandler : IRequestHandler<PurgeDirectoryRequest, Result<PurgeDirectoryResponse>>
{
    private readonly ILogger<PurgeDirectoryHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public PurgeDirectoryHandler(ILogger<PurgeDirectoryHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<PurgeDirectoryResponse>> Handle(PurgeDirectoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storagePluginNormsParser.Parse(request.Path);
            await _storageService.PurgeDirectoryAsync(storageNorms, cancellationToken);
            return await Result<PurgeDirectoryResponse>.SuccessAsync(Resources.PurgeDirectoryHandlerSuccessfullyPurged);
        }
        catch (Exception ex)
        {
            return await Result<PurgeDirectoryResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}