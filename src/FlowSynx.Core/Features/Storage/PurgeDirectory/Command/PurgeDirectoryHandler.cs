using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;

namespace FlowSynx.Core.Features.Storage.PurgeDirectory.Command;

internal class PurgeDirectoryHandler : IRequestHandler<PurgeDirectoryRequest, Result<PurgeDirectoryResponse>>
{
    private readonly ILogger<PurgeDirectoryHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public PurgeDirectoryHandler(ILogger<PurgeDirectoryHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<PurgeDirectoryResponse>> Handle(PurgeDirectoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storageNormsParser.Parse(request.Path);
            await _storageService.PurgeDirectoryAsync(storageNorms, cancellationToken);
            return await Result<PurgeDirectoryResponse>.SuccessAsync(Resources.PurgeDirectoryHandlerSuccessfullyPurged);
        }
        catch (Exception ex)
        {
            return await Result<PurgeDirectoryResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}