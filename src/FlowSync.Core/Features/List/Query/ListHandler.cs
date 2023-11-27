using MediatR;
using FlowSync.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSync.Abstractions.Entities;
using FlowSync.Core.FileSystem;
using FlowSync.Core.FileSystem.Filter;
using FlowSync.Abstractions.Helpers;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Common.Utilities;
using EnsureThat;
using FlowSync.Abstractions.Models;
using FlowSync.Core.Configuration;

namespace FlowSync.Core.Features.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<ListResponse>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IFileSystemService _fileSystem;

    public ListHandler(ILogger<ListHandler> logger, IFileSystemService fileSystem)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(fileSystem, nameof(fileSystem));
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<Result<IEnumerable<ListResponse>>> Handle(ListRequest request, CancellationToken cancellationToken)
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

            var entities = await _fileSystem.List(request.Path, filters, cancellationToken);
            var response = entities.Select(x => new ListResponse()
            {
                Id = x.Id,
                Kind = x.Kind.ToString().ToLower(),
                Name = x.Name,
                Path = x.FullPath,
                ModifiedTime = x.ModifiedTime,
                Size = ByteSizeHelper.FormatByteSize(x.Size, request.FormatSize),
                MimeType = x.MimeType
            });

            return await Result<IEnumerable<ListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<ListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}