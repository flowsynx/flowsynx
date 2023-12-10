using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using EnsureThat;
using FlowSync.Core.Storage;
using FlowSync.Core.Parers.Norms.Storage;

namespace FlowSync.Core.Features.Storage.Read.Query;

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
            var entities = await _storageService.ReadAsync(storageNorms, cancellationToken);

            var response = new ReadResponse()
            {
                Content = entities
            };

            return await Result<ReadResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ReadResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}