using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO;

namespace FlowSynx.Core.Features.Compress.Command;

internal class CompressHandler : IRequestHandler<CompressRequest, Result<CompressResult>>
{
    private readonly ILogger<CompressHandler> _logger;
    private readonly IConnectorParser _connectorParser;
    private readonly Func<CompressType, ICompression> _compressionFactory;

    public CompressHandler(ILogger<CompressHandler> logger, IConnectorParser connectorParser,
        Func<CompressType, ICompression> compressionFactory)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
        _compressionFactory = compressionFactory;
    }

    public async Task<Result<CompressResult>> Handle(CompressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var connector = _connectorParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();

            var compressType = string.IsNullOrEmpty(request.CompressType)
                ? CompressType.Zip
                : EnumUtils.GetEnumValueOrDefault<CompressType>(request.CompressType)!.Value;

            //var context = new Context(options);
            //var compressEntries = connector.Specifications;// await connector.CompressAsync(context, cancellationToken);

            //var enumerable = compressEntries.ToList();
            //if (!enumerable.Any())
            //    throw new Exception(Resources.NoDataToCompress);

            //var compressResult = await _compressionFactory(compressType).Compress(enumerable);
            //var response = new CompressResult
            //    { Content = compressResult.Content, ContentType = compressResult.ContentType };

            var response = new CompressResult
            { Content = "compressResult.Content".ToByteArray() };

            return await Result<CompressResult>.SuccessAsync(response, Resources.CompressHandlerSuccessfullyCompress);
        }
        catch (Exception ex)
        {
            return await Result<CompressResult>.FailAsync(new List<string> { ex.Message });
        }
    }
}