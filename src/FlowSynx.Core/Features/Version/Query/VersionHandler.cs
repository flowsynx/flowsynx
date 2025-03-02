//using MediatR;
//using Microsoft.Extensions.Logging;
//using EnsureThat;
//using FlowSynx.Abstractions;
//using FlowSynx.Environment;

//namespace FlowSynx.Core.Features.Version.Query;

//internal class VersionHandler : IRequestHandler<VersionRequest, Result<VersionResponse>>
//{
//    private readonly ILogger<VersionHandler> _logger;
//    private readonly IVersion _version;
//    private readonly IOperatingSystemInfo _operatingSystemInfo;

//    public VersionHandler(ILogger<VersionHandler> logger, IVersion version, IOperatingSystemInfo operatingSystemInfo)
//    {
//        EnsureArg.IsNotNull(logger, nameof(logger));
//        EnsureArg.IsNotNull(version, nameof(version));
//        EnsureArg.IsNotNull(operatingSystemInfo, nameof(operatingSystemInfo));
//        _logger = logger;
//        _version = version;
//        _operatingSystemInfo = operatingSystemInfo;
//    }

//    public async Task<Result<VersionResponse>> Handle(VersionRequest request, CancellationToken cancellationToken)
//    {
//        try
//        {
//            var response = new VersionResponse()
//            {
//                FlowSynx = _version.Version,
//                OSVersion = _operatingSystemInfo.Version,
//                OSType = _operatingSystemInfo.Type,
//                OSArchitecture = _operatingSystemInfo.Architecture
//            };

//            return await Result<VersionResponse>.SuccessAsync(response);
//        }
//        catch (Exception ex)
//        {
//            return await Result<VersionResponse>.FailAsync(new List<string> { ex.Message });
//        }
//    }
//}