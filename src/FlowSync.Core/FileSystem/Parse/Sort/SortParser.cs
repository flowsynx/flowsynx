using FlowSync.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace FlowSync.Core.FileSystem.Parse.Sort;

internal class SortParser : ISortParser
{
    private readonly ILogger<ISortParser> _logger;

    public SortParser(ILogger<ISortParser> logger)
    {
        _logger = logger;
    }

    public List<SortInfo> Parse(string sortStatement)
    {
        if (string.IsNullOrEmpty(sortStatement))
            sortStatement = "name asc";

        return ParseSortWithSuffix(sortStatement);
    }

    #region internal Methods
    protected List<SortInfo> ParseSortWithSuffix(string sortStatement)
    {
        var items = sortStatement.Split(',').Select(p => p.Trim());
        return items.Select(ParseSortTerms).ToList();
    }
    
    private SortInfo ParseSortTerms(string item)
    {
        var pair = item.Split(' ').Select(p => p.Trim()).ToList();
        if (pair.Count > 2)
        {
            _logger.LogError($"Invalid OrderBy string '{item}'. Order By Format: Property, Property2 ASC, Property2 DESC");
            throw new SortParserException($"Invalid Sorting string '{item}'. Order By Format: Property, Property2 ASC, Property2 DESC");
        }

        var prop = pair[0];

        if (string.IsNullOrEmpty(prop))
            throw new SortParserException("Invalid Property. Order By Format: Property, Property2 ASC, Property2 DESC");

        var name = NormalizePropertyName(prop);

        var direction = SortDirection.Ascending;
        if (pair.Count == 2)
            direction = NormalizeSortDirection(pair[1]);

        return new SortInfo { Name = name, Direction = direction };
    }

    private string NormalizePropertyName(string propertyName)
    {
        string[] terms = {"Kind", "Name", "Size", "Age"};
        if (!terms.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogError($"Invalid Property. '{terms}' is not valid.");
            throw new SortParserException($"Invalid Property. Given sorting property name '{propertyName}' is not valid.");
        }

        if (string.Equals(propertyName, "age", StringComparison.OrdinalIgnoreCase))
            propertyName = "CreatedTime";

        return propertyName;
    }

    private SortDirection NormalizeSortDirection(string sortDirection)
    {
        string[] terms = { "Asc", "Desc" };

        if (terms.Contains(sortDirection, StringComparer.OrdinalIgnoreCase)) 
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? SortDirection.Descending : SortDirection.Ascending;

        _logger.LogWarning($"Sort direction for {sortDirection} is not valid.");
        throw new SortParserException($"Sort direction for {sortDirection} is not valid.");
    }
    #endregion
}