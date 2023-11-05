namespace FlowSync.Core.FileSystem.Parse.Size;

internal interface ISizeParser
{
    long Parse(string size);
}