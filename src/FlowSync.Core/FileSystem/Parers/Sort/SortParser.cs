using Microsoft.Extensions.Logging;
using FlowSync.Core.Exceptions;
using EnsureThat;

namespace FlowSync.Core.FileSystem.Parers.Sort;

internal class SortParser : ISortParser
{
    private readonly ILogger<SortParser> _logger;

    public SortParser(ILogger<SortParser> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public List<SortInfo> Parse(string sortStatement, string[] properties)
    {
        if (string.IsNullOrEmpty(sortStatement))
            sortStatement = "name asc";

        return ParseSortWithSuffix(sortStatement, properties);
    }

    #region internal Methods
    protected List<SortInfo> ParseSortWithSuffix(string sortStatement, string[] properties)
    {
        var items = sortStatement.Split(',').Select(p => p.Trim());
        return items.Select(x => ParseSortTerms(x, properties)).ToList();
    }

    private SortInfo ParseSortTerms(string item, string[] properties)
    {
        var pair = item.Split(' ').Select(p => p.Trim()).ToList();
        if (pair.Count > 2)
        {
            _logger.LogError($"Invalid OrderBy string '{item}'. Order By Format: Property, Property2 ASC, Property2 DESC");
            throw new SortParserException(string.Format(FlowSyncCoreResource.FileSystemSortParserInvalidSortingTerm, item));
        }

        var prop = pair[0];

        if (string.IsNullOrEmpty(prop))
            throw new SortParserException(FlowSyncCoreResource.FileSystemSortParserInvalidProperty);

        var name = NormalizePropertyName(prop, properties);

        var direction = SortDirection.Ascending;
        if (pair.Count == 2)
            direction = NormalizeSortDirection(pair[1], name);

        return new SortInfo { Name = name, Direction = direction };
    }

    private string NormalizePropertyName(string propertyName, string[] properties)
    {
        if (!properties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogError($"Invalid Property. '{properties}' is not valid.");
            throw new SortParserException(string.Format(FlowSyncCoreResource.FileSystemSortParserInvalidPropertyName, propertyName));
        }

        if (string.Equals(propertyName, "age", StringComparison.OrdinalIgnoreCase))
            propertyName = "CreatedTime";

        return propertyName;
    }

    private SortDirection NormalizeSortDirection(string sortDirection, string property)
    {
        string[] terms = { "Asc", "Desc" };

        if (terms.Contains(sortDirection, StringComparer.OrdinalIgnoreCase))
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? SortDirection.Descending : SortDirection.Ascending;

        _logger.LogWarning($"Sort direction '{sortDirection}' for '{property}' is not valid.");
        throw new SortParserException(string.Format(FlowSyncCoreResource.FileSystemSortParserInvalidSortDirection, sortDirection, property));
    }
    #endregion
}