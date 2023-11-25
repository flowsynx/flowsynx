namespace FlowSync.Core.FileSystem.Parse.RemotePath;

internal interface IRemotePathParser
{
    RemotePathResult Parse(string path);
}