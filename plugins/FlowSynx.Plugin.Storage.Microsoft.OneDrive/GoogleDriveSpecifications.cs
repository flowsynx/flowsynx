using FlowSynx.Commons;
using FlowSynx.Plugin.Abstractions;
using System;
using System.ComponentModel;
using System.Reflection;

namespace FlowSynx.Plugin.Storage.Google.Drive;

internal class GoogleDriveSpecifications
{
    [RequiredMember]
    public string? ProjectId { get; set; }

    [RequiredMember]
    public string? PrivateKeyId { get; set; }

    [RequiredMember]
    public string? PrivateKey { get; set; }

    [RequiredMember]
    public string? ClientEmail { get; set; }

    [RequiredMember]
    public string? ClientId { get; set; }

    [RequiredMember]
    public string? FolderId { get; set; }

    public string? Scope { get; set; } = "Drive";

    public string ApplicationScope => GetScope(Scope);

    public string Type => "service_account";

    public string AuthUri => "https://accounts.google.com/o/oauth2/auth";

    public string TokenUri => "https://oauth2.googleapis.com/token";

    public string AuthProviderX509CertUrl => "https://www.googleapis.com/oauth2/v1/certs";

    public string ClientX509CertUrl => $"https://www.googleapis.com/robot/v1/metadata/x509/{ClientEmail}";

    public string UniverseDomain => "googleapis.com";

    private string GetScope(string? scope)
    {
        var value = string.IsNullOrEmpty(scope)
            ? GoogleDriveScope.DriveReadonly
            : EnumUtils.GetEnumValueOrDefault<GoogleDriveScope>(scope)!.Value;

        return GetScopeValue(value);
    }

    private string GetScopeValue(GoogleDriveScope scope)
    {
        var description = StringValueOf(scope);
        return string.IsNullOrEmpty(description) ? string.Empty : $"https://www.googleapis.com/auth/{Scope}";
    }

    public string StringValueOf(Enum value)
    {
        var fi = value.GetType().GetField(value.ToString());
        if (fi == null)
            return string.Empty;

        DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }
}