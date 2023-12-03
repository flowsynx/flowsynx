using FlowSync.Core.Extensions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSync.Abstractions.Common.Attributes;
using FlowSync.Abstractions.Storage;
using FlowSync.Core.Parers.Date;
using FlowSync.Core.Parers.Size;
using FlowSync.Core.Parers.Sort;

namespace FlowSync.Core.Storage.Filter;

internal class StorageFilter: IStorageFilter
{
    private readonly ILogger<StorageFilter> _logger;
    private readonly IDateParser _dateParser;
    private readonly ISizeParser _sizeParser;
    private readonly ISortParser _sortParser;

    public StorageFilter(ILogger<StorageFilter> logger, IDateParser dateParser, ISizeParser sizeParser, ISortParser sortParser)
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

    public IEnumerable<StorageEntity> FilterEntitiesList(IEnumerable<StorageEntity> entities, StorageSearchOptions storageSearchOptions)
    {
        var predicate = PredicateBuilder.True<StorageEntity>();

        if (!string.IsNullOrEmpty(storageSearchOptions.Include))
        {
            var myRegex = new Regex(storageSearchOptions.Include, storageSearchOptions.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            predicate = predicate.And(d => myRegex.IsMatch(d.Name));
        }
        if (!string.IsNullOrEmpty(storageSearchOptions.Exclude) && string.IsNullOrEmpty(storageSearchOptions.Include))
        {
            var myRegex = new Regex(storageSearchOptions.Exclude, storageSearchOptions.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            predicate = predicate.And(d => !myRegex.IsMatch(d.Name));
        }
        if (!string.IsNullOrEmpty(storageSearchOptions.MinimumAge))
        {
            var parsedDateTime = _dateParser.Parse(storageSearchOptions.MinimumAge);
            predicate = predicate.And(p => p.CreatedTime >= parsedDateTime);
        }
        if (!string.IsNullOrEmpty(storageSearchOptions.MaximumAge))
        {
            var parsedDateTime = _dateParser.Parse(storageSearchOptions.MaximumAge);
            predicate = predicate.And(p => p.CreatedTime <= parsedDateTime);
        }
        if (!string.IsNullOrEmpty(storageSearchOptions.MinimumSize))
        {
            var parsedSize = _sizeParser.Parse(storageSearchOptions.MinimumSize);
            predicate = predicate.And(p => p.Size >= parsedSize && p.Kind == StorageEntityItemKind.File);
        }
        if (!string.IsNullOrEmpty(storageSearchOptions.MaximumSize))
        {
            var parsedSize = _sizeParser.Parse(storageSearchOptions.MaximumSize);
            predicate = predicate.And(p => p.Size <= parsedSize && p.Kind == StorageEntityItemKind.File);
        }

        var result = entities.Where(predicate.Compile());

        if (!string.IsNullOrEmpty(storageSearchOptions.Sorting))
        {
            var parsedSort = _sortParser.Parse(storageSearchOptions.Sorting, ObjectPropertiesList<StorageEntity>());
            result = result.Sorting(parsedSort);
        }
        
        return result;
    }

    protected IEnumerable<string> ObjectPropertiesList<T>()
    {
        try
        {
            var properties = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SortMemberAttribute))).ToList();
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