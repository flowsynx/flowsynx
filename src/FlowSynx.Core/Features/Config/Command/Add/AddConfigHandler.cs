using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;
using FlowSynx.Core.Features.Storage.List.Query;

namespace FlowSynx.Core.Features.Config.Command.Add;

internal class AddConfigHandler : IRequestHandler<AddConfigRequest, Result<AddConfigResponse>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IConfigurationManager _configurationManager;

    public AddConfigHandler(ILogger<ListHandler> logger, IConfigurationManager configurationManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public async Task<Result<AddConfigResponse>> Handle(AddConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var configItem = new ConfigurationItem
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Type = request.Type,
                CreatedTime = DateTimeOffset.Now,
                ModifiedTime = DateTimeOffset.Now,
                Specifications = request.Specifications,
            };

            var isExist = _configurationManager.IsExist(request.Name);
            if (isExist)
            {
                return await Result<AddConfigResponse>.FailAsync(string.Format(Resources.AddConfigHandlerItemIsAlreadyExist, request.Name));
            }

            var result = _configurationManager.Add(configItem);
            var response = new AddConfigResponse
            {
                Id = result.Id, 
                Name = configItem.Name
            };
            return await Result<AddConfigResponse>.SuccessAsync(response, Resources.AddConfigHandlerSuccessfullyAdded);
        }
        catch (Exception ex)
        {
            return await Result<AddConfigResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}