namespace FlowSync.Infrastructure.IO;

public interface IFileWriter
{
    public bool Write(string path, string contents);
}
