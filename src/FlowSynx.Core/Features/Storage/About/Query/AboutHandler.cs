using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;
using FlowSynx.Formatting;

namespace FlowSynx.Core.Features.Storage.About.Query;

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
                Total = entities.Total.ToString(!request.Full),
                Free = entities.Free.ToString(!request.Full),
                Used = entities.Used.ToString(!request.Full)
            };

            return await Result<AboutResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<AboutResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}