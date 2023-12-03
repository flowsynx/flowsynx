namespace FlowSync.Core.Parers.Size;

public interface ISizeParser : IParser
{
    long Parse(string size);
}