using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using EnsureThat;
using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FlowSync.Core.Storage;
using FlowSync.Core.Parers.Norms.Storage;

namespace FlowSync.Core.Features.Storage.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<DeleteResponse>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public DeleteHandler(ILogger<DeleteHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<DeleteResponse>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storagePath = _storageNormsParser.Parse(request.Path);
            var filters = new StorageSearchOptions()
            {
                Kind = string.IsNullOrEmpty(request.Kind) ? StorageFilterItemKind.FileAndDirectory : EnumUtils.GetEnumValueOrDefault<StorageFilterItemKind>(request.Kind)!.Value,
                Include = request.Include,
                Exclude = request.Exclude,
                MinimumAge = request.MinAge,
                MaximumAge = request.MaxAge,
                MinimumSize = request.MinSize,
                MaximumSize = request.MaxSize,
                Sorting = request.Sorting,
                CaseSensitive = request.CaseSensitive ?? false,
                Recurse = request.Recurse ?? false
            };

            await _storageService.Delete(storagePath, filters, cancellationToken);
            return await Result<DeleteResponse>.SuccessAsync("The files deleted successfully.");
        }
        catch (Exception ex)
        {
            return await Result<DeleteResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}