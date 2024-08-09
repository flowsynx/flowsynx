using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<ListResponse>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public ListHandler(ILogger<ListHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<IEnumerable<ListResponse>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storagePluginNormsParser.Parse(request.Path);
            var searchOptions = new StorageSearchOptions()
            {
                Include = request.Include,
                Exclude = request.Exclude,
                MinimumAge = request.MinAge,
                MaximumAge = request.MaxAge,
                MinimumSize = request.MinSize,
                MaximumSize = request.MaxSize,
                CaseSensitive = request.CaseSensitive ?? false,
                Recurse = request.Recurse ?? false,
            };

            var listOptions = new StorageListOptions()
            {
                Kind = string.IsNullOrEmpty(request.Kind) ? 
                    StorageFilterItemKind.FileAndDirectory : 
                    EnumUtils.GetEnumValueOrDefault<StorageFilterItemKind>(request.Kind)!.Value,
                Sorting = request.Sorting,
                MaxResult = request.MaxResults
            };

            var hashOptions = new StorageHashOptions()
            {
                Hashing = request.Hashing
            };

            var metadataOptions = new StorageMetadataOptions()
            {
                IncludeMetadata = request.IncludeMetadata
            };

            var entities = await _storageService.List(storageNorms, searchOptions, 
                listOptions, hashOptions, metadataOptions, cancellationToken);

            var storageEntities = entities.ToList();
            var result = new List<ListResponse>(storageEntities.Count());

            result.AddRange(storageEntities.Select(entity => new ListResponse
            {
                Id = entity.Id,
                Kind = entity.Kind.ToString().ToLower(),
                Name = entity.Name,
                Path = entity.FullPath,
                ModifiedTime = entity.ModifiedTime,
                Size = entity.Size.ToString(!request.Full),
                ContentType = entity.ContentType,
                Md5 = entity.Md5,
                Metadata = entity.Metadata
            }));

            return await Result<IEnumerable<ListResponse>>.SuccessAsync(result);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<ListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}