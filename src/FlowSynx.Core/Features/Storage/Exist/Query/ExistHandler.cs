using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.Exist.Query;

internal class ExistHandler : IRequestHandler<ExistRequest, Result<ExistResponse>>
{
    private readonly ILogger<ExistHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public ExistHandler(ILogger<ExistHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<ExistResponse>> Handle(ExistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storagePluginNormsParser.Parse(request.Path);
            var isExist = await _storageService.FileExist(storageNorms, cancellationToken);
            var response = new ExistResponse()
            {
                Exist = isExist
            };

            return await Result<ExistResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ExistResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}