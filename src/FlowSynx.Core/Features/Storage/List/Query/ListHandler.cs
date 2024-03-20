using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;
using FlowSynx.Formatting;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Features.Storage.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<ListResponse>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public ListHandler(ILogger<ListHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<IEnumerable<ListResponse>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _storageNormsParser.Parse(request.Path);
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

            var entities = await _storageService.List(storageNorms, searchOptions, 
                listOptions, hashOptions, cancellationToken);

            var response = entities.Select(x => new ListResponse()
            {
                Id = x.Id,
                Kind = x.Kind.ToString().ToLower(),
                Name = x.Name,
                Path = x.FullPath,
                ModifiedTime = x.ModifiedTime,
                Size = x.Size.ToString(!request.Full),
                MimeType = x.MimeType,
                Md5 = x.Md5,
            });

            return await Result<IEnumerable<ListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<ListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}