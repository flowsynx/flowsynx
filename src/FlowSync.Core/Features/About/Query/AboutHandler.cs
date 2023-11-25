using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.FileSystem;
using FlowSync.Abstractions.Helpers;
using FlowSync.Core.Common.Models;
using EnsureThat;

namespace FlowSync.Core.Features.About.Query;

internal class AboutHandler : IRequestHandler<AboutRequest, Result<AboutResponse>>
{
    private readonly ILogger<AboutHandler> _logger;
    private readonly IFileSystemService _fileSystem;

    public AboutHandler(ILogger<AboutHandler> logger, IFileSystemService fileSystem)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(fileSystem, nameof(fileSystem));
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<Result<AboutResponse>> Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entities = await _fileSystem.About(request.Path, cancellationToken);
            var response = new AboutResponse()
            {
                Total = ByteSizeHelper.FormatByteSize(entities.Total, request.FormatSize),
                Free = ByteSizeHelper.FormatByteSize(entities.Free, request.FormatSize),
                Used = ByteSizeHelper.FormatByteSize(entities.Used, request.FormatSize)
            };

            return await Result<AboutResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<AboutResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}