using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Services;
using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Version.Inquiry;

internal class VersionHandler : IActionHandler<VersionInquiry, Result<VersionResult>>
{
    private readonly ILogger<VersionHandler> _logger;
    private readonly IVersion _version;

    public VersionHandler(ILogger<VersionHandler> logger, IVersion version)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public async Task<Result<VersionResult>> Handle(VersionInquiry request, CancellationToken cancellationToken)
    {
        try
        {
            var response = new VersionResult()
            {
                Version = _version.Version.ToString()
            };

            return await Result<VersionResult>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<VersionResult>.FailAsync(ex.ToString());
        }
    }
}