using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Features.Storage.Read.Query;

internal class ReadHandler : IRequestHandler<ReadRequest, Result<ReadResponse>>
{
    private readonly ILogger<ReadHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public ReadHandler(ILogger<ReadHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<ReadResponse>> Handle(ReadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storageNormsParser.Parse(request.Path);

            var hashOptions = new StorageHashOptions()
            {
                Hashing = request.Hashing
            };

            var entity = await _storageService.ReadAsync(storageNorms, hashOptions, cancellationToken);

            var response = new ReadResponse()
            {
                Content = entity.Stream,
                Extension = entity.Extension,
                MimeType = entity.MimeType,
                Md5 = request.Hashing is true ? entity.Md5 : null
            };

            return await Result<ReadResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ReadResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}