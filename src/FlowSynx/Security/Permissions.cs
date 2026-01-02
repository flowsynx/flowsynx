namespace FlowSynx.Security;

public static class Permissions
{
    public const string Admin = "admin";
    public const string User = "user";
    public const string Audits = "audits";
    public const string Config = "config";
    public const string Logs = "logs";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Admin, User, Audits, Config, Logs
    };
}
