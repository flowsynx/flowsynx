namespace FlowSync.Core.Parers.Sort;

public interface ISortParser : IParser
{
    List<SortInfo> Parse(string sortStatement, IEnumerable<string> properties);
}