using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Compress.Command;

internal class CompressHandler : IRequestHandler<CompressRequest, Result<CompressResult>>
{
    private readonly ILogger<CompressHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;
    private readonly Func<CompressType, ICompression> _compressionFactory;

    public CompressHandler(ILogger<CompressHandler> logger, IPluginContextParser pluginContextParser, 
        Func<CompressType, ICompression> compressionFactory)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
        _compressionFactory = compressionFactory;
    }

    public async Task<Result<CompressResult>> Handle(CompressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();

            var compressType = string.IsNullOrEmpty(request.CompressType)
                ? CompressType.Zip
                : EnumUtils.GetEnumValueOrDefault<CompressType>(request.CompressType)!.Value;

            var compressEntries = await contex.InvokePlugin.CompressAsync(contex.Entity, contex.InferiorPlugin, 
                options, cancellationToken);

            var enumerable = compressEntries.ToList();
            if (!enumerable.Any())
                throw new Exception(Resources.NoDataToCompress);

            var compressResult = await _compressionFactory(compressType).Compress(enumerable);
            var response = new CompressResult { Content = compressResult.Content, ContentType = compressResult.ContentType };

            return await Result<CompressResult>.SuccessAsync(response, Resources.CompressHandlerSuccessfullyCompress);
        }
        catch (Exception ex)
        {
            return await Result<CompressResult>.FailAsync(new List<string> { ex.Message });
        }
    }
}