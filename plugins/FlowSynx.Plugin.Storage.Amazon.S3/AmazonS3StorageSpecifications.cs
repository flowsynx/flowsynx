using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Storage.Amazon.S3;

internal class AmazonS3StorageSpecifications
{
    [RequiredMember]
    public string? AccessKey { get; set; }

    [RequiredMember]
    public string? SecretKey { get; set; }

    [RequiredMember]
    public string? Region { get; set; }

    public string? SessionToken { get; set; }

    //public string? Endpoint => string.IsNullOrEmpty(Region) ? null : $"s3.{Region.ToLower()}.amazonaws.com";
}