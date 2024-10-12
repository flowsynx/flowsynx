using System.ComponentModel;

namespace FlowSynx.Connectors.Storage.Google.Drive;

internal enum GoogleDriveScope
{
    [Description("drive.readonly")]
    DriveReadonly = 0,
    [Description("drive")]
    Drive = 1,
    [Description("drive.file")]
    DriveFile,
    [Description("drive.metadata.readonly")]
    DriveMetadataReadonly
}