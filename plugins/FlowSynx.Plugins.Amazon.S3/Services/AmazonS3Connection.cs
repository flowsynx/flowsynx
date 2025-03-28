using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FlowSynx.Plugins.Amazon.S3.Models;

namespace FlowSynx.Plugins.Amazon.S3.Services;

public class AmazonS3Connection : IAmazonS3Connection
{
    public AmazonS3Client Connect(AmazonS3Specifications specifications)
    {
        if (specifications.AccessKey == null)
            throw new ArgumentNullException(nameof(specifications.AccessKey));

        if (specifications.SecretKey == null)
            throw new ArgumentNullException(nameof(specifications.SecretKey));

        var awsCredentials = string.IsNullOrEmpty(specifications.SessionToken)
            ? (AWSCredentials)new BasicAWSCredentials(specifications.AccessKey, specifications.SecretKey)
            : new SessionAWSCredentials(specifications.AccessKey, specifications.SecretKey, specifications.SessionToken);

        var config = GetConfig(specifications.Region);
        return new AmazonS3Client(awsCredentials, config);
    }

    private AmazonS3Config GetConfig(string? region)
    {
        if (region == null)
            return new AmazonS3Config();

        return new AmazonS3Config
        {
            RegionEndpoint = ToRegionEndpoint(region),
        };
    }

    private RegionEndpoint ToRegionEndpoint(string? region)
    {
        if (region is null)
            throw new ArgumentNullException(nameof(region));

        return RegionEndpoint.GetBySystemName(region);
    }
}