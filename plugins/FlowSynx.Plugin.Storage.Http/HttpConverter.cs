using System.Text.RegularExpressions;

namespace FlowSynx.Plugin.Storage.Http;

static class HttpConverter
{
    public static StorageEntity ToEntity(Uri uri, GroupCollection groups, StorageEntityItemKind kind)
    {
        string uriWithoutScheme = uri.Host + uri.Port + uri.PathAndQuery + uri.Fragment;
        string name = kind == StorageEntityItemKind.File ? groups["file"].ToString() : groups["dir"].ToString();
        var entity = new StorageEntity(name, kind)
        {

        };

        return entity;
    }
}