using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<DeleteResponse>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public DeleteHandler(ILogger<DeleteHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<DeleteResponse>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storagePluginNormsParser.Parse(request.Path);
            var filters = new StorageSearchOptions()
            {
                Include = request.Include,
                Exclude = request.Exclude,
                MinimumAge = request.MinAge,
                MaximumAge = request.MaxAge,
                MinimumSize = request.MinSize,
                MaximumSize = request.MaxSize,
                CaseSensitive = request.CaseSensitive ?? false,
                Recurse = request.Recurse ?? false
            };

            await _storageService.Delete(storageNorms, filters, cancellationToken);
            return await Result<DeleteResponse>.SuccessAsync(Resources.DeleteHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<DeleteResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}