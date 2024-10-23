using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Google.Drive.Models;
using FlowSynx.IO.Serialization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace FlowSynx.Connectors.Storage.Google.Drive.Services;

public class GoogleDriveConnection: IGoogleDriveConnection
{
    private readonly ISerializer _serializer;

    public GoogleDriveConnection(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public DriveService GetClient(GoogleDriveSpecifications specifications)
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
            universe_domain = specifications.UniverseDomain,
        };

        var json = _serializer.Serialize(jsonObject);
        var credential = GoogleCredential.FromJson(json);

        if (credential == null)
            throw new StorageException(Resources.ErrorInCreateDriveServiceCredential);

        if (credential.IsCreateScopedRequired)
        {
            string[] scopes = {
                DriveService.Scope.Drive,
                DriveService.Scope.DriveMetadataReadonly,
                DriveService.Scope.DriveFile,
            };
            credential = credential.CreateScoped(scopes);
        }

        var driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        return driveService;
    }
}