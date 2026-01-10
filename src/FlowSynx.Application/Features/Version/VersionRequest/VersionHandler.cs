using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Version.VersionRequest;

internal class VersionHandler : IActionHandler<VersionRequest, Result<VersionResult>>
{
    private readonly ILogger<VersionHandler> _logger;
    private readonly IVersion _version;

    public VersionHandler(ILogger<VersionHandler> logger, IVersion version)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public async Task<Result<VersionResult>> Handle(VersionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = new VersionResult()
            {
                Version = _version.Version.ToString()
            };

            return await Result<VersionResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<VersionResult>.FailAsync(ex.Message);
        }
    }
}