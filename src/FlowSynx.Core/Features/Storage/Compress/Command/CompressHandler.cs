using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage;
using FlowSynx.Plugin.Storage;
using FlowSynx.Commons;
using FlowSynx.Core.Services;
using FlowSynx.Core.Storage.Compress;
using FlowSynx.IO.Compression;
using SharpCompress.Common;

namespace FlowSynx.Core.Features.Storage.Compress.Command;

internal class CompressHandler : IRequestHandler<CompressRequest, Result<CompressResponse>>
{
    private readonly ILogger<CompressHandler> _logger;
    private readonly IStorageService _storageService;
    private readonly IStorageNormsParser _storageNormsParser;

    public CompressHandler(ILogger<CompressHandler> logger, IStorageService storageService, IStorageNormsParser storageNormsParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _storageNormsParser = storageNormsParser;
    }

    public async Task<Result<CompressResponse>> Handle(CompressRequest request, CancellationToken cancellationToken)
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
                Recurse = request.Recurse ?? false
            };

            var listOptions = new StorageListOptions()
            {
                Kind = string.IsNullOrEmpty(request.Kind) 
                    ? StorageFilterItemKind.FileAndDirectory 
                    : EnumUtils.GetEnumValueOrDefault<StorageFilterItemKind>(request.Kind)!.Value,
                MaxResult = request.MaxResults
            };

            var hashOptions = new StorageHashOptions()
            {
                Hashing = request.Hashing
            };

            var compressionOptions = new StorageCompressionOptions()
            {
                CompressType = string.IsNullOrEmpty(request.CompressType)
                    ? CompressType.Zip
                    : EnumUtils.GetEnumValueOrDefault<CompressType>(request.CompressType)!.Value
            };

            var result = await _storageService.Compress(storageNorms, searchOptions, listOptions, 
                hashOptions, compressionOptions, cancellationToken);

            var response = new CompressResponse()
            {
                Content = result.Stream,
                ContentType = result.ContentType,
                Md5 = result.Md5
            };
            
            return await Result<CompressResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<CompressResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}