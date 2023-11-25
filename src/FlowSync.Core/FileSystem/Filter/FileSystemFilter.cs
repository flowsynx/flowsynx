using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions;
using FlowSync.Core.Extensions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using FlowSync.Core.FileSystem.Parers.Date;
using FlowSync.Core.FileSystem.Parers.Size;
using FlowSync.Core.FileSystem.Parers.Sort;

namespace FlowSync.Core.FileSystem.Filter;

internal class FileSystemFilter: IFileSystemFilter
{
    private readonly ILogger<FileSystemFilter> _logger;
    private readonly IDateParser _dateParser;
    private readonly ISizeParser _sizeParser;
    private readonly ISortParser _sortParser;
    private readonly string[] _properties = new string[] { "Id", "Kind", "Name", "Size", "MimeType", "ModifiedTime" };

    public FileSystemFilter(ILogger<FileSystemFilter> logger, IDateParser dateParser, ISizeParser sizeParser, ISortParser sortParser)
    {
        _logger = logger;
        _dateParser = dateParser;
        _sizeParser = sizeParser;
        _sortParser = sortParser;
    }

    public IEnumerable<FileSystemEntity> FilterList(IEnumerable<FileSystemEntity> entities, FileSystemFilterOptions fileSystemFilterOptions)
    {
        var predicate = PredicateBuilder.True<FileSystemEntity>();

        if (!string.IsNullOrEmpty(fileSystemFilterOptions.Include))
        {
            var myRegex = new Regex(fileSystemFilterOptions.Include, fileSystemFilterOptions.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            predicate = predicate.And(d => myRegex.IsMatch(d.Name));
        }
        if (!string.IsNullOrEmpty(fileSystemFilterOptions.Exclude) && string.IsNullOrEmpty(fileSystemFilterOptions.Include))
        {
            var myRegex = new Regex(fileSystemFilterOptions.Exclude, fileSystemFilterOptions.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            predicate = predicate.And(d => !myRegex.IsMatch(d.Name));
        }
        if (!string.IsNullOrEmpty(fileSystemFilterOptions.MinimumAge))
        {
            var parsedDateTime = _dateParser.Parse(fileSystemFilterOptions.MinimumAge);
            predicate = predicate.And(p => p.CreatedTime >= parsedDateTime);
        }
        if (!string.IsNullOrEmpty(fileSystemFilterOptions.MaximumAge))
        {
            var parsedDateTime = _dateParser.Parse(fileSystemFilterOptions.MaximumAge);
            predicate = predicate.And(p => p.CreatedTime <= parsedDateTime);
        }
        if (!string.IsNullOrEmpty(fileSystemFilterOptions.MinimumSize))
        {
            var parsedSize = _sizeParser.Parse(fileSystemFilterOptions.MinimumSize);
            predicate = predicate.And(p => p.Size >= parsedSize && p.Kind == EntityItemKind.File);
        }
        if (!string.IsNullOrEmpty(fileSystemFilterOptions.MaximumSize))
        {
            var parsedSize = _sizeParser.Parse(fileSystemFilterOptions.MaximumSize);
            predicate = predicate.And(p => p.Size <= parsedSize && p.Kind == EntityItemKind.File);
        }

        var result = entities.Where(predicate.Compile());

        if (!string.IsNullOrEmpty(fileSystemFilterOptions.Sorting))
        {
            var parsedSort = _sortParser.Parse(fileSystemFilterOptions.Sorting, _properties);
            result = result.Sorting(parsedSort);
        }

        if (fileSystemFilterOptions.MaxResults > 0)
            result = result.Take(fileSystemFilterOptions.MaxResults);

        return result;
    }
}