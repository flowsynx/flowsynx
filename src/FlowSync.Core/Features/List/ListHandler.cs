using MediatR;
using FlowSync.Core.Wrapper;
using FlowSync.Core.Extensions;
using FlowSync.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSync.Abstractions.Entities;
using FlowSync.Core.Utilities;
using FlowSync.Core.FileSystem;
using FlowSync.Core.FileSystem.Filter;

namespace FlowSync.Core.Features.List;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<ListResponse>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IFileSystemService _fileSystem;
    private readonly IFilter _filter;

    public ListHandler(ILogger<ListHandler> logger, IFileSystemService fileSystem, IFilter filter)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _filter = filter;
    }

    public async Task<Result<IEnumerable<ListResponse>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var filters = new FilterOptions()
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

            if (!entities.Succeeded)
                return await Result<IEnumerable<ListResponse>>.FailAsync(entities.Messages);

            var filteredData = _filter.FilterList(entities.Data, filters);

            var x = filteredData.Select(x => new ListResponse()
            {
                Kind = x.Kind.ToString().ToLower(),
                Name = x.Name,
                DateCreated = x.CreatedTime,
                Size = x.Size,
            });

            return await Result<IEnumerable<ListResponse>>.SuccessAsync(x);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return await Result<IEnumerable<ListResponse>>.FailAsync(new List<string> {ex.Message});
        }
    }
}