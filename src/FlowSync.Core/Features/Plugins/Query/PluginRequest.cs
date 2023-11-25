using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Plugins.Query;

public class PluginRequest : IRequest<Result<IEnumerable<PluginResponse>>>
{

}