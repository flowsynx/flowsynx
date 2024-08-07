﻿using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.Size.Query;

internal class SizeHandler : IRequestHandler<SizeRequest, Result<SizeResponse>>
{
    private readonly ILogger<SizeHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public SizeHandler(ILogger<SizeHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<SizeResponse>> Handle(SizeRequest request, CancellationToken cancellationToken)
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
                Recurse = request.Recurse ?? false
            };

            var listOptions = new StorageListOptions()
            {
                Kind = string.IsNullOrEmpty(request.Kind) ? 
                    StorageFilterItemKind.FileAndDirectory : 
                    EnumUtils.GetEnumValueOrDefault<StorageFilterItemKind>(request.Kind)!.Value,
                Sorting = null,
                MaxResult = request.MaxResults
            };

            var hashOptions = new StorageHashOptions()
            {
                Hashing = false
            };

            var metadataOptions = new StorageMetadataOptions()
            {
                IncludeMetadata = false
            };

            var entities = await _storageService.List(storageNorms, searchOptions, 
                listOptions, hashOptions, metadataOptions, cancellationToken);

            var response = new SizeResponse()
            {
                Size = entities.Sum(x => x.Size).ToString(!request.Full),
            };

            return await Result<SizeResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<SizeResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}