using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.PluginInstancing;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Compress.Command;

internal class CompressHandler : IRequestHandler<CompressRequest, Result<CompressResult>>
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

    public async Task<Result<CompressResult>> Handle(CompressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginInstance = _pluginInstanceParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();

            var compressType = string.IsNullOrEmpty(request.CompressType)
                ? CompressType.Zip
                : EnumUtils.GetEnumValueOrDefault<CompressType>(request.CompressType)!.Value;

            var compressEntries = await _storageService.CompressAsync(pluginInstance, options, cancellationToken);

            var enumerable = compressEntries.ToList();
            if (!enumerable.Any())
                throw new Exception(Resources.NoDataToCompress);

            var compressResult = await _compressionFactory(compressType).Compress(enumerable);
            var response = new CompressResult { Content = compressResult.Stream, ContentType = compressResult.ContentType };

            return await Result<CompressResult>.SuccessAsync(response, Resources.CompressHandlerSuccessfullyCompress);
        }
        catch (Exception ex)
        {
            return await Result<CompressResult>.FailAsync(new List<string> { ex.Message });
        }
    }
}