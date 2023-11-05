namespace FlowSync.Persistence.Json.IO;

public interface IFileWriter
{
    public bool Write(string path, string contents);
}
