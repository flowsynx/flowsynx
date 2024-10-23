using FlowSynx.Connectors.Storage.Google.Cloud.Models;
using FlowSynx.IO.Serialization;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace FlowSynx.Connectors.Storage.Google.Cloud.Services;

public class GoogleCloudConnection: IGoogleCloudConnection
{
    private readonly ISerializer _serializer;

    public GoogleCloudConnection(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public StorageClient GetClient(GoogleCloudSpecifications specifications)
    {
        var jsonObject = new
        {
            type = specifications.Type,
            project_id = specifications.ProjectId,
            private_key_id = specifications.PrivateKeyId,
            private_key = specifications.PrivateKey,
            client_email = specifications.ClientEmail,
            client_id = specifications.ClientId,
            auth_uri = specifications.AuthUri,
            token_uri = specifications.TokenUri,
            auth_provider_x509_cert_url = specifications.AuthProviderX509CertUrl,
            client_x509_cert_url = specifications.ClientX509CertUrl,
            universe_domain = specifications.UniverseDomain
        };

        var json = _serializer.Serialize(jsonObject);
        var credential = GoogleCredential.FromJson(json);
        return StorageClient.Create(credential);
    }
}