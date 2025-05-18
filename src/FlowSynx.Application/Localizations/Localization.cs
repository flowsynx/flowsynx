namespace FlowSynx.Application.Localizations;

public static class Localization
{
    public static ILocalization? Instance { get; set; }

    public static string Get(string key)
    {
        if (Instance == null)
            throw new ArgumentNullException(nameof(Instance));

        return Instance.Get(key);
    }

    public static string Get(string key, params object[] args)
    { 
        if (Instance == null)
            throw new ArgumentNullException(nameof(Instance));

        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        return Instance.Get(key, args); 
    }
}