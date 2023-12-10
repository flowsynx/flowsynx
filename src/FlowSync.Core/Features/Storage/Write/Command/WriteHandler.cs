using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using EnsureThat;
using FlowSync.Abstractions.Storage;
using FlowSync.Core.Storage;
using FlowSync.Core.Parers.Norms.Storage;
using FlowSync.Core.Extensions;

namespace FlowSync.Core.Features.Storage.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<WriteResponse>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public WriteHandler(ILogger<WriteHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<WriteResponse>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storageNormsParser.Parse(request.Path);
            var stream = request.Data.IsBase64String() ? request.Data.Base64ToStream() : request.Data.ToStream();
            var storageStream = new StorageStream(stream);
            await _storageService.WriteAsync(storageNorms, storageStream, cancellationToken);
            return await Result<WriteResponse>.SuccessAsync("The file was write successfully.");
        }
        catch (Exception ex)
        {
            return await Result<WriteResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}