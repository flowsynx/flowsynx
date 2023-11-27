namespace FlowSync.Abstractions.Parers.Sort;

public interface ISortParser
{
    List<SortInfo> Parse(string sortStatement, IEnumerable<string> properties);
}