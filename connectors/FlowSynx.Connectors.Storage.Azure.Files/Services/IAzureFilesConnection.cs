using Azure.Storage.Files.Shares;
using FlowSynx.Connectors.Storage.Azure.Files.Models;

namespace FlowSynx.Connectors.Storage.Azure.Files.Services;

public interface IAzureFilesConnection
{
    ShareClient GetClient(AzureFilesSpecifications specifications);

}