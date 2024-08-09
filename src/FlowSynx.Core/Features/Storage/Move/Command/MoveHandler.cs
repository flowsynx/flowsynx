using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Features.Storage.List.Query;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using FlowSynx.Plugin.Storage.Move;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.Move.Command;

internal class MoveHandler : IRequestHandler<MoveRequest, Result<MoveResponse>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public MoveHandler(ILogger<ListHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<MoveResponse>> Handle(MoveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sourceStorageNorms = _storagePluginNormsParser.Parse(request.SourcePath);
            var destinationStorageNorms = _storagePluginNormsParser.Parse(request.DestinationPath);

            var searchOptions = new StorageSearchOptions()
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

            var moveOptions = new StorageMoveOptions() { };

            await _storageService.Move(sourceStorageNorms, destinationStorageNorms, searchOptions, moveOptions, cancellationToken);
            return await Result<MoveResponse>.SuccessAsync(Resources.MoveHandlerSuccessfullyMoved);
        }
        catch (Exception ex)
        {
            return await Result<MoveResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}