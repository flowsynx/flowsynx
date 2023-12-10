using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using EnsureThat;
using FlowSync.Core.Storage;
using FlowSync.Core.Parers.Norms.Storage;

namespace FlowSync.Core.Features.Storage.DeleteFile.Command;

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
            return await Result<DeleteFileResponse>.SuccessAsync("The file was deleted successfully.");
        }
        catch (Exception ex)
        {
            return await Result<DeleteFileResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}