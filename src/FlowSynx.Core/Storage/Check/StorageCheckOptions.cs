namespace FlowSynx.Core.Storage.Check;

public class StorageCheckOptions
{
    public bool? CheckSize { get; set; } = true;
    public bool? CheckHash { get; set; } = false;
    public bool? OneWay { get; set; } = false;
}