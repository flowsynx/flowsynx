using FlowSynx.Connectors.Storage.Google.Cloud.Models;
using Google.Cloud.Storage.V1;

namespace FlowSynx.Connectors.Storage.Google.Cloud.Services;

public interface IGoogleCloudConnection
{
    StorageClient GetClient(GoogleCloudSpecifications specifications);
}