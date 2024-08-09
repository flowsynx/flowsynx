using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions.Models;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<WriteResponse>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public WriteHandler(ILogger<WriteHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<WriteResponse>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storagePluginNormsParser.Parse(request.Path);
            var stream = request.Data.IsBase64String() ? request.Data.Base64ToStream() : request.Data.ToStream();
            var storageStream = new StorageStream(stream);
            var writeOptions = new StorageWriteOptions {Overwrite = request.Overwrite};
            await _storageService.WriteAsync(storageNorms, storageStream, writeOptions, cancellationToken);
            return await Result<WriteResponse>.SuccessAsync(Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<WriteResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}