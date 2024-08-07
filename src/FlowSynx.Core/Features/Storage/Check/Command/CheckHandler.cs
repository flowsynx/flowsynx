﻿using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using FlowSynx.Plugin.Storage.Check;
using FlowSynx.Plugin.Storage.Services;

namespace FlowSynx.Core.Features.Storage.Check.Command;

internal class CheckHandler : IRequestHandler<CheckRequest, Result<IEnumerable<CheckResponse>>>
{
    private readonly ILogger<CheckHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStoragePluginNormsParser _storagePluginNormsParser;

    public CheckHandler(ILogger<CheckHandler> logger, IStorageService storageService, IStoragePluginNormsParser storagePluginNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storagePluginNormsParser = storagePluginNormsParser;
    }

    public async Task<Result<IEnumerable<CheckResponse>>> Handle(CheckRequest request, CancellationToken cancellationToken)
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

            var checkOptions = new StorageCheckOptions()
            {
                CheckSize = request.CheckSize,
                CheckHash = request.CheckHash,
                OneWay = request.OneWay
            };

            var result = await _storageService.Check(sourceStorageNorms, destinationStorageNorms, searchOptions, checkOptions, cancellationToken);
            var checkResults = result.ToList();
            var response = new List<CheckResponse>(checkResults.Count());
            response.AddRange(checkResults.Select(item => new CheckResponse
            {
                Id = item.Entity.Id,
                Name = item.Entity.Name,
                Path = item.Entity.FullPath,
                State = item.State.ToString()
            }));
            return await Result<IEnumerable<CheckResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<CheckResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}