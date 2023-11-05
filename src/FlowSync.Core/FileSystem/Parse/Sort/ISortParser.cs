namespace FlowSync.Core.FileSystem.Parse.Sort;

internal interface ISortParser
{
    List<SortInfo> Parse(string sortStatement);
}