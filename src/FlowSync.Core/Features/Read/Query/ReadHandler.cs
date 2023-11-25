using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.FileSystem;
using FlowSync.Core.Common.Models;
using EnsureThat;

namespace FlowSync.Core.Features.Read.Query;

internal class ReadHandler : IRequestHandler<ReadRequest, Result<ReadResponse>>
{
    private readonly ILogger<ReadHandler> _logger;
    private readonly IFileSystemService _fileSystem;

    public ReadHandler(ILogger<ReadHandler> logger, IFileSystemService fileSystem)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(fileSystem, nameof(fileSystem));
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<Result<ReadResponse>> Handle(ReadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entities = await _fileSystem.ReadAsync(request.Path, cancellationToken);

            var response = new ReadResponse()
            {
                Content = entities
            };

            return await Result<ReadResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ReadResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}