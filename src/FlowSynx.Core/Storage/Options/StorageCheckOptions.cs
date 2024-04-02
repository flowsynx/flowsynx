namespace FlowSynx.Core.Storage.Options;

public class StorageCheckOptions
{
    public bool? CheckSize { get; set; } = true;
    public bool? CheckHash { get; set; } = false;
    public bool? OneWay { get; set; } = false;
}