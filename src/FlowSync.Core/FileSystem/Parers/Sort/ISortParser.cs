namespace FlowSync.Core.FileSystem.Parers.Sort;

internal interface ISortParser
{
    List<SortInfo> Parse(string sortStatement, string[] properties);
}