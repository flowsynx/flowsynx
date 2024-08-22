using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.PluginInstancing;

namespace FlowSynx.Core.Features.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public DeleteHandler(ILogger<DeleteHandler> logger, IPluginService pluginService, IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        _logger = logger;
        _pluginService = pluginService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<IEnumerable<object>>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _pluginInstanceParser.Parse(request.Path);
            var options = new PluginFilters()
            {

            };

            //var filters = new StorageSearchOptions()
            //{
            //    Include = request.Include,
            //    Exclude = request.Exclude,
            //    MinimumAge = request.MinAge,
            //    MaximumAge = request.MaxAge,
            //    MinimumSize = request.MinSize,
            //    MaximumSize = request.MaxSize,
            //    CaseSensitive = request.CaseSensitive ?? false,
            //    Recurse = request.Recurse ?? false
            //};

            var response = await _pluginService.DeleteAsync(storageNorms, options, cancellationToken);
            return await Result<IEnumerable<object>>.SuccessAsync(response, Resources.DeleteHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}