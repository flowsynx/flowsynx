using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Version.Query;

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
        catch (Exception ex)
        {
            return await Result<VersionResponse>.FailAsync(ex.Message);
        }
    }
}