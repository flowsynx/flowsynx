using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using EnsureThat;
using FlowSync.Abstractions.Common.Helpers;
using FlowSync.Core.Storage;
using FlowSync.Core.Parers.Norms.Storage;

namespace FlowSync.Core.Features.Storage.About.Query;

internal class AboutHandler : IRequestHandler<AboutRequest, Result<AboutResponse>>
{
    private readonly ILogger<AboutHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public AboutHandler(ILogger<AboutHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<AboutResponse>> Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storageNormsParser.Parse(request.Path);
            var entities = await _storageService.About(storageNorms, cancellationToken);
            var response = new AboutResponse()
            {
                Total = ByteFormat.ToString(entities.Total, !request.Full),
                Free = ByteFormat.ToString(entities.Free, !request.Full),
                Used = ByteFormat.ToString(entities.Used, !request.Full)
            };

            return await Result<AboutResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<AboutResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}