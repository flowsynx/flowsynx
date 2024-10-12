using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Create.Command;

internal class CreateHandler : IRequestHandler<CreateRequest, Result<Unit>>
{
    private readonly ILogger<CreateHandler> _logger;
    private readonly IContextParser _contextParser;

    public CreateHandler(ILogger<CreateHandler> logger, IContextParser contextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contextParser = contextParser;
    }

    public async Task<Result<Unit>> Handle(CreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contextParser.Parse(request.Entity);
            var options = request.Options.ToConnectorOptions();
            await contex.CurrentConnector.CreateAsync(contex.Entity, contex.NextConnector, options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.CreateHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}