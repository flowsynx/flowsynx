using FlowSynx.Abstractions.Attributes;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Storage.Google.Cloud;

internal class GoogleCloudStorageSpecifications: Specifications
{
    [RequiredMember]
    public string ProjectId { get; set; } = string.Empty;

    [RequiredMember]
    public string PrivateKeyId { get; set; } = string.Empty;

    [RequiredMember]
    public string PrivateKey { get; set; } = string.Empty;

    [RequiredMember]
    public string ClientEmail { get; set; } = string.Empty;

    [RequiredMember]
    public string ClientId { get; set; } = string.Empty;

    public string Type => "service_account";

    public string AuthUri => "https://accounts.google.com/o/oauth2/auth";

    public string TokenUri => "https://oauth2.googleapis.com/token";

    public string AuthProviderX509CertUrl => "https://www.googleapis.com/oauth2/v1/certs";

    public string ClientX509CertUrl => $"https://www.googleapis.com/robot/v1/metadata/x509/{ClientEmail}";

    public string UniverseDomain => "googleapis.com";
}