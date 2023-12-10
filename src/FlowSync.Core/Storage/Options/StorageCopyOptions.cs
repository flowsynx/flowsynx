namespace FlowSync.Core.Storage.Options;

public class StorageCopyOptions
{
    public bool? ClearDestinationPath { get; set; } = false;
    public bool? OverWriteData { get; set; } = false;

}