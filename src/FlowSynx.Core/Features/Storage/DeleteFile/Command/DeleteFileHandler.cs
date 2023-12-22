using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;

namespace FlowSynx.Core.Features.Storage.DeleteFile.Command;

internal class DeleteFileHandler : IRequestHandler<DeleteFileRequest, Result<DeleteFileResponse>>
{
    private readonly ILogger<DeleteFileHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public DeleteFileHandler(ILogger<DeleteFileHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<DeleteFileResponse>> Handle(DeleteFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storageNormsParser.Parse(request.Path);
            await _storageService.DeleteFile(storageNorms, cancellationToken);
            return await Result<DeleteFileResponse>.SuccessAsync(Resources.DeleteFileHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<DeleteFileResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}