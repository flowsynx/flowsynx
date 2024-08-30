using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.PluginInstancing;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin;

namespace FlowSynx.Core.Features.Compress.Command;

internal class CompressHandler : IRequestHandler<CompressRequest, Result<object>>
{
    private readonly ILogger<CompressHandler> _logger;
    private readonly IPluginService _storageService;
    private readonly IPluginInstanceParser _pluginInstanceParser;
    private readonly Func<CompressType, ICompression> _compressionFactory;

    public CompressHandler(ILogger<CompressHandler> logger, IPluginService storageService, 
        IPluginInstanceParser pluginInstanceParser, Func<CompressType, ICompression> compressionFactory)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _pluginInstanceParser = pluginInstanceParser;
        _compressionFactory = compressionFactory;
    }

    public async Task<Result<object>> Handle(CompressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginInstance = _pluginInstanceParser.Parse(request.Entity);
            var filters = request.Filters.ToPluginFilters();

            var compressType = string.IsNullOrEmpty(request.CompressType)
                ? CompressType.Zip
                : EnumUtils.GetEnumValueOrDefault<CompressType>(request.CompressType)!.Value;

            var compressEntries = await _storageService.CompressAsync(pluginInstance, filters, cancellationToken);

            if (compressEntries is not List<CompressEntry> entries)
                throw new Exception(Resources.ErrorDuringCompressData);

            if (!entries.Any())
                throw new Exception(Resources.NoDataToCompress);

            var compressResult = await _compressionFactory(compressType).Compress(entries);
            var response = new CompressResult { Content = compressResult.Stream, ContentType = compressResult.ContentType };

            return await Result<object>.SuccessAsync(response, Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}