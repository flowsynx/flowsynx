using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.IO;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Storage;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.PluginInstancing;

namespace FlowSynx.Core.Features.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<object>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IPluginService _storageService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public WriteHandler(ILogger<WriteHandler> logger, IPluginService storageService, IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<object>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginInstance = _pluginInstanceParser.Parse(request.Entity);
            var filters = request.Filters.ToPluginFilters();
            var dataStream = request.Data.IsBase64String() ? request.Data.Base64ToStream() : request.Data.ToStream();
            var stream = new StorageStream(dataStream);
            var response = await _storageService.WriteAsync(pluginInstance, filters, stream, cancellationToken);
            return await Result<object>.SuccessAsync(response, Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}