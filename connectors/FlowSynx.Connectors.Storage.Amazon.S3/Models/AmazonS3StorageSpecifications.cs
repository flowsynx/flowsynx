using FlowSynx.Abstractions.Attributes;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Models;

public class AmazonS3StorageSpecifications : Specifications
{
    [RequiredMember]
    public string AccessKey { get; set; } = string.Empty;

    [RequiredMember]
    public string SecretKey { get; set; } = string.Empty;

    [RequiredMember]
    public string Region { get; set; } = string.Empty;

    public string SessionToken { get; set; } = string.Empty;
}