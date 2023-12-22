using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;
using FlowSynx.IO;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Features.Storage.Write.Command;

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
            return await Result<WriteResponse>.SuccessAsync(Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<WriteResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}