﻿using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.List.Query;

public class ListRequest : IRequest<Result<IEnumerable<object>>>
{
    public required string Entity { get; set; }
    public PluginFilters? Filters { get; set; } = new PluginFilters();

}