namespace FlowSynx.Connectors.Storage.Google.Drive;

internal class GoogleDrivePath
{
    public GoogleDrivePath(bool exist, string id)
    {
        Exist = exist;
        Id = id;
    }

    public bool Exist { get; }
    public string Id { get; }
}