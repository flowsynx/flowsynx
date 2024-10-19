using Amazon.S3;
using FlowSynx.Connectors.Storage.Amazon.S3.Models;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public interface IAmazonS3ClientHandler
{
    AmazonS3Client GetClient(AmazonS3Specifications specifications);
}