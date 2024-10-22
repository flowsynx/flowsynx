namespace FlowSynx.Connectors.Storage.Google.Drive.Models;

internal class GoogleDrivePathPart
{
    public GoogleDrivePathPart(bool exist, string id)
    {
        Exist = exist;
        Id = id;
    }

    public bool Exist { get; }
    public string Id { get; }
}