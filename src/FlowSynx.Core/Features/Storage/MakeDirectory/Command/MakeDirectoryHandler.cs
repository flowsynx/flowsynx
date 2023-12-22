using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;

namespace FlowSynx.Core.Features.Storage.MakeDirectory.Command;

internal class MakeDirectoryHandler : IRequestHandler<MakeDirectoryRequest, Result<MakeDirectoryResponse>>
{
    private readonly ILogger<MakeDirectoryHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public MakeDirectoryHandler(ILogger<MakeDirectoryHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<MakeDirectoryResponse>> Handle(MakeDirectoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storageNormsParser.Parse(request.Path);
            await _storageService.MakeDirectoryAsync(storageNorms, cancellationToken);
            return await Result<MakeDirectoryResponse>.SuccessAsync(Resources.MakeDirectoryHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<MakeDirectoryResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}