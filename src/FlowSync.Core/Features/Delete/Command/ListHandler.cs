using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Abstractions.Entities;
using FlowSync.Core.FileSystem;
using FlowSync.Abstractions.Helpers;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Common.Utilities;
using EnsureThat;
using FlowSync.Abstractions.Models;

namespace FlowSync.Core.Features.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<DeleteResponse>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IFileSystemService _fileSystem;

    public DeleteHandler(ILogger<DeleteHandler> logger, IFileSystemService fileSystem)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(fileSystem, nameof(fileSystem));
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<Result<DeleteResponse>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var filters = new FileSystemFilterOptions()
            {
                Kind = string.IsNullOrEmpty(request.Kind) ? FilterItemKind.FileAndDirectory : EnumUtils.GetEnumValueOrDefault<FilterItemKind>(request.Kind)!.Value,
                Include = request.Include,
                Exclude = request.Exclude,
                MinimumAge = request.MinAge,
                MaximumAge = request.MaxAge,
                MinimumSize = request.MinSize,
                MaximumSize = request.MaxSize,
                Sorting = request.Sorting,
                CaseSensitive = request.CaseSensitive ?? false,
                Recurse = request.Recurse ?? false,
                MaxResults = request.MaxResults ?? 10
            };

            await _fileSystem.Delete(request.Path, filters, cancellationToken);
            return await Result<DeleteResponse>.SuccessAsync("The files deleted successfully.");
        }
        catch (Exception ex)
        {
            return await Result<DeleteResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}