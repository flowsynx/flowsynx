﻿using FlowSynx.Application.Extensions;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Query.Details;

internal class PluginDetailsHandler : IRequestHandler<PluginDetailsRequest, Result<PluginDetailsResponse>>
{
    private readonly ILogger<PluginDetailsHandler> _logger;
    private readonly IPluginService _pluginService;

    public PluginDetailsHandler(ILogger<PluginDetailsHandler> logger, IPluginService pluginService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        _logger = logger;
        _pluginService = pluginService;
    }

    public async Task<Result<PluginDetailsResponse>> Handle(PluginDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginId = Guid.Parse(request.Id);
            var plugin = await _pluginService.Get(pluginId, cancellationToken);
            var specificationsType = plugin.SpecificationsType;
            var properties = specificationsType.GetProperties().Where(x => x.CanWrite).ToList();
            var specifications = properties
                .Select(property => new PluginDetailsSpecification
                {
                    Key = property.Name,
                    Type = property.PropertyType.GetPrimitiveType(),
                    Required = Attribute.IsDefined(property, typeof(RequiredMemberAttribute))
                }).ToList();

            var response = new PluginDetailsResponse
            {
                Id = plugin.Id,
                Type = plugin.Type,
                Description = plugin.Description,
                Specifications = specifications
            };

            return await Result<PluginDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<PluginDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}