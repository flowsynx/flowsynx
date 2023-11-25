namespace FlowSync.Core.FileSystem.Parers.RemotePath;

internal interface IRemotePathParser
{
    RemotePathResult Parse(string path);
}