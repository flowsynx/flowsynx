using Azure.Storage.Files.Shares;
using FlowSynx.Connectors.Storage.Azure.Files.Models;

namespace FlowSynx.Connectors.Storage.Azure.Files.Services;

public interface IAzureFilesClientHandler
{
    ShareClient GetClient(AzureFilesSpecifications specifications);

}