namespace FlowSync.Abstractions.Storage;

public class StorageUsage
{
    public long Total { get; set; }
    public long Free { get; set; }
    public long Used => Total - Free;
}