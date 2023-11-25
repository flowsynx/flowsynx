namespace FlowSync.Core.FileSystem.Parers.Size;

internal interface ISizeParser
{
    long Parse(string size);
}