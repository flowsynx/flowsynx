using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions;
using FlowSync.Core.Extensions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using FlowSync.Core.FileSystem.Parse.Date;
using FlowSync.Core.FileSystem.Parse.Size;
using FlowSync.Core.FileSystem.Parse.Sort;

namespace FlowSync.Core.FileSystem.Filter;

internal class Filter: IFilter
{
    private readonly ILogger<Filter> _logger;
    private readonly IDateParser _dateParser;
    private readonly ISizeParser _sizeParser;
    private readonly ISortParser _sortParser;

    public Filter(ILogger<Filter> logger, IDateParser dateParser, ISizeParser sizeParser, ISortParser sortParser)
    {
        _logger = logger;
        _dateParser = dateParser;
        _sizeParser = sizeParser;
        _sortParser = sortParser;
    }

    public IEnumerable<Entity> FilterList(IEnumerable<Entity> entities, FilterOptions filterOptions)
    {
        var predicate = PredicateBuilder.True<Entity>();

        if (!string.IsNullOrEmpty(filterOptions.Include))
        {
            var myRegex = new Regex(filterOptions.Include, filterOptions.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            predicate = predicate.And(d => myRegex.IsMatch(d.Name));
        }
        if (!string.IsNullOrEmpty(filterOptions.MinimumAge))
        {
            var parsedDateTime = _dateParser.Parse(filterOptions.MinimumAge);
            predicate = predicate.And(p => p.CreatedTime >= parsedDateTime);
        }
        if (!string.IsNullOrEmpty(filterOptions.MaximumAge))
        {
            var parsedDateTime = _dateParser.Parse(filterOptions.MaximumAge);
            predicate = predicate.And(p => p.CreatedTime <= parsedDateTime);
        }
        if (!string.IsNullOrEmpty(filterOptions.MinimumSize))
        {
            var parsedSize = _sizeParser.Parse(filterOptions.MinimumSize);
            predicate = predicate.And(p => p.Size >= parsedSize && p.Kind == EntityItemKind.File);
        }
        if (!string.IsNullOrEmpty(filterOptions.MaximumSize))
        {
            var parsedSize = _sizeParser.Parse(filterOptions.MaximumSize);
            predicate = predicate.And(p => p.Size <= parsedSize && p.Kind == EntityItemKind.File);
        }

        var result = entities.Where(predicate.Compile());

        if (!string.IsNullOrEmpty(filterOptions.Sorting))
        {
            var parsedSort = _sortParser.Parse(filterOptions.Sorting);
            result = result.Sorting(parsedSort);
        }

        return result;
    }
}