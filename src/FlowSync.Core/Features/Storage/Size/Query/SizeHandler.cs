using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using EnsureThat;
using FlowSync.Abstractions.Storage;
using FlowSync.Abstractions.Common.Helpers;
using FlowSync.Core.Common;
using FlowSync.Core.Storage;
using FlowSync.Core.Parers.Norms.Storage;

namespace FlowSync.Core.Features.Storage.Size.Query;

internal class SizeHandler : IRequestHandler<SizeRequest, Result<SizeResponse>>
{
    private readonly ILogger<SizeHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public SizeHandler(ILogger<SizeHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<SizeResponse>> Handle(SizeRequest request, CancellationToken cancellationToken)
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

            var maxResults = request.MaxResults;
            var entities = await _storageService.List(storagePath, filters, maxResults, cancellationToken);
            var response = new SizeResponse()
            {
                Size = ByteFormat.ToString(entities.Sum(x => x.Size), request.FormatSize),
            };

            return await Result<SizeResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<SizeResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}