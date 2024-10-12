using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Abstractions.Attributes;
using FlowSynx.Core.Extensions;
using FlowSynx.Reflections;
using FlowSynx.Connectors.Manager;

namespace FlowSynx.Core.Features.Connectors.Query.Details;

internal class ConnectorDetailsHandler : IRequestHandler<ConnectorDetailsRequest, Result<ConnectorDetailsResponse>>
{
    private readonly ILogger<ConnectorDetailsHandler> _logger;
    private readonly IConnectorsManager _connectorsManager;

    public ConnectorDetailsHandler(ILogger<ConnectorDetailsHandler> logger, IConnectorsManager connectorsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(connectorsManager, nameof(connectorsManager));
        _logger = logger;
        _connectorsManager = connectorsManager;
    }

    public async Task<Result<ConnectorDetailsResponse>> Handle(ConnectorDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var connector = _connectorsManager.Get(request.Type);
            var specificationsType = connector.SpecificationsType;
            var properties = specificationsType.Properties().Where(x=>x.CanWrite).ToList();
            var specifications = properties
                .Select(property => new ConnectorDetailsSpecification
                {
                    Key = property.Name, 
                    Type = property.PropertyType.GetPrimitiveType(), 
                    Required = Attribute.IsDefined(property, typeof(RequiredMemberAttribute))
                }).ToList();

            var response = new ConnectorDetailsResponse
            {
                Id = connector.Id,
                Type = connector.Type,
                Description = connector.Description,
                Specifications = specifications
            };

            return await Result<ConnectorDetailsResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ConnectorDetailsResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}