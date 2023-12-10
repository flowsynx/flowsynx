using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using EnsureThat;
using FlowSync.Core.Storage;
using FlowSync.Core.Parers.Norms.Storage;

namespace FlowSync.Core.Features.Storage.PurgeDirectory.Command;

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
            return await Result<PurgeDirectoryResponse>.SuccessAsync("The directory was purged successfully.");
        }
        catch (Exception ex)
        {
            return await Result<PurgeDirectoryResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}