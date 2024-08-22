﻿using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.About.Query;

public class AboutRequest : IRequest<Result<object>>
{
    public string Entity { get; set; } = string.Empty;
    public PluginFilters? Filters { get; set; } = new PluginFilters();
}