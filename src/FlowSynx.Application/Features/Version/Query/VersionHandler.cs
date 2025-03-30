using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Version.Query;

internal class VersionHandler : IRequestHandler<VersionRequest, Result<VersionResponse>>
{
    private readonly ILogger<VersionHandler> _logger;
    private readonly IVersion _version;

    public VersionHandler(ILogger<VersionHandler> logger, IVersion version)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(version);
        _logger = logger;
        _version = version;
    }

    public async Task<Result<VersionResponse>> Handle(VersionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = new VersionResponse()
            {
                Version = _version.Version
            };

            return await Result<VersionResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<VersionResponse>.FailAsync(ex.ToString());
        }
    }
}