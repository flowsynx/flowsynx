using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Core.Extensions;
using FlowSynx.Parsers.Date;
using FlowSynx.Parsers.Extensions;
using FlowSynx.Parsers.Size;
using FlowSynx.Parsers.Sort;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Storage;
using FlowSynx.Parsers.Percent;

namespace FlowSynx.Core.Storage.Filter;

internal class StorageFilter: IStorageFilter
{
    private readonly ILogger<StorageFilter> _logger;
    private readonly IDateParser _dateParser;
    private readonly ISizeParser _sizeParser;
    private readonly ISortParser _sortParser;
    private readonly IPercentParser _percentParser;

    public StorageFilter(ILogger<StorageFilter> logger, IDateParser dateParser, 
        ISizeParser sizeParser, ISortParser sortParser, IPercentParser percentParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dateParser, nameof(dateParser));
        EnsureArg.IsNotNull(sizeParser, nameof(sizeParser));
        EnsureArg.IsNotNull(sortParser, nameof(sortParser));
        EnsureArg.IsNotNull(percentParser, nameof(percentParser));
        _logger = logger;
        _dateParser = dateParser;
        _sizeParser = sizeParser;
        _sortParser = sortParser;
        _percentParser = percentParser;
    }

    public IEnumerable<StorageEntity> FilterEntitiesList(IEnumerable<StorageEntity> entities, StorageSearchOptions storageSearchOptions, StorageListOptions listOptions)
    {
        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            return storageEntities;
        
        var predicate = PredicateBuilder.True<StorageEntity>();

        switch (listOptions.Kind)
        {
            case StorageFilterItemKind.File:
                predicate = predicate.And(p => p.Kind == StorageEntityItemKind.File);
                break;
            case StorageFilterItemKind.Directory:
                predicate = predicate.And(p => p.Kind == StorageEntityItemKind.Directory);
                break;
            case StorageFilterItemKind.FileAndDirectory:
            default:
                break;
        }

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

        var result = storageEntities.Where(predicate.Compile());

        if (!string.IsNullOrEmpty(listOptions.Sorting))
        {
            var parsedSort = _sortParser.Parse(listOptions.Sorting, ObjectPropertiesList<StorageEntity>());
            result = result.Sorting(parsedSort);
        }

        var maxResult = _percentParser.Parse(listOptions.MaxResult, storageEntities.Count());
        if (maxResult > 0)
            result = result.Take(maxResult);
        
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