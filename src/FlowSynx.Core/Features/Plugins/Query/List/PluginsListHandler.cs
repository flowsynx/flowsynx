﻿using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Plugins.Query.List;

internal class PluginsListHandler : IRequestHandler<PluginsListRequest, Result<IEnumerable<PluginsListResponse>>>
{
    private readonly ILogger<PluginsListHandler> _logger;
    private readonly IPluginsManager _pluginsManager;

    public PluginsListHandler(ILogger<PluginsListHandler> logger, IPluginsManager pluginsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        _logger = logger;
        _pluginsManager = pluginsManager;
    }

    public async Task<Result<IEnumerable<PluginsListResponse>>> Handle(PluginsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<IPlugin> plugins;
            if (string.IsNullOrEmpty(request.Namespace))
                plugins = _pluginsManager.Plugins();
            else
            {
                var @namespace = EnumUtils.GetEnumValueOrDefault<PluginNamespace>(request.Namespace)!.Value;
                plugins = _pluginsManager.Plugins(@namespace);
            }

            var response = plugins.Select(x => new PluginsListResponse
            {
                Id = x.Id,
                Type = x.Type,
                Description = x.Description
            });

            return await Result<IEnumerable<PluginsListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<PluginsListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}