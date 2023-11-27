using FlowSync.Abstractions.Entities;
using FlowSync.Core.Extensions;
using System.Text.RegularExpressions;
using FlowSync.Abstractions.Filter;
using Microsoft.Extensions.Logging;
using FlowSync.Abstractions.Models;
using FlowSync.Abstractions.Parers.Date;
using FlowSync.Abstractions.Parers.Size;
using FlowSync.Abstractions.Parers.Sort;
using EnsureThat;
using FlowSync.Abstractions;

namespace FlowSync.Core.FileSystem.Filter;

internal class FileSystemFilter: IFileSystemFilter
{
    private readonly ILogger<FileSystemFilter> _logger;
    private readonly IDateParser _dateParser;
    private readonly ISizeParser _sizeParser;
    private readonly ISortParser _sortParser;

    public FileSystemFilter(ILogger<FileSystemFilter> logger, IDateParser dateParser, ISizeParser sizeParser, ISortParser sortParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dateParser, nameof(dateParser));
        EnsureArg.IsNotNull(sizeParser, nameof(sizeParser));
        EnsureArg.IsNotNull(sortParser, nameof(sortParser));
        _logger = logger;
        _dateParser = dateParser;
        _sizeParser = sizeParser;
        _sortParser = sortParser;
    }

    public IEnumerable<FileSystemEntity> FilterEntitiesList(IEnumerable<FileSystemEntity> entities, FileSystemFilterOptions fileSystemFilterOptions)
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
            var parsedSort = _sortParser.Parse(fileSystemFilterOptions.Sorting, ObjectPropertiesList<FileSystemEntity>());
            result = result.Sorting(parsedSort);
        }

        if (fileSystemFilterOptions.MaxResults > 0)
            result = result.Take(fileSystemFilterOptions.MaxResults);

        return result;
    }

    protected IEnumerable<string> ObjectPropertiesList<T>()
    {
        try
        {
            var properties = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(FilterMemberAttribute))).ToList();
            if (!properties.Any())
                properties = typeof(T).GetProperties().ToList();

            return properties.Select(x => x.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.Message);
            return new List<string>();
        }
    }
}