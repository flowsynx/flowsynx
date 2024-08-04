using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Dropbox;

internal class DropboxStorageSpecifications
{
    [RequiredMember]
    public string AccessToken { get; set; } = string.Empty;
}