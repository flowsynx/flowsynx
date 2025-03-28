using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Amazon.S3.Models;

public class AmazonS3Specifications : PluginSpecifications
{
    [RequiredMember]
    public string AccessKey { get; set; } = string.Empty;

    [RequiredMember]
    public string SecretKey { get; set; } = string.Empty;

    [RequiredMember]
    public string Region { get; set; } = string.Empty;

    [RequiredMember]
    public string Bucket { get; set; } = string.Empty;

    public string SessionToken { get; set; } = string.Empty;
}