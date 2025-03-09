using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Http;

public class HttpSpecifications
{
    [RequiredMember]
    public string? Url { get; set; }
}