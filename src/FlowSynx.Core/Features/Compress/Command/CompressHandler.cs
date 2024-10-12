using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Compress.Command;

internal class CompressHandler : IRequestHandler<CompressRequest, Result<CompressResult>>
{
    private readonly ILogger<CompressHandler> _logger;
    private readonly IContextParser _contextParser;
    private readonly Func<CompressType, ICompression> _compressionFactory;

    public CompressHandler(ILogger<CompressHandler> logger, IContextParser contextParser, 
        Func<CompressType, ICompression> compressionFactory)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contextParser = contextParser;
        _compressionFactory = compressionFactory;
    }

    public async Task<Result<CompressResult>> Handle(CompressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contextParser.Parse(request.Entity);
            var options = request.Options.ToConnectorOptions();

            var compressType = string.IsNullOrEmpty(request.CompressType)
                ? CompressType.Zip
                : EnumUtils.GetEnumValueOrDefault<CompressType>(request.CompressType)!.Value;

            var compressEntries = await contex.CurrentConnector.CompressAsync(contex.Entity, contex.NextConnector, 
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