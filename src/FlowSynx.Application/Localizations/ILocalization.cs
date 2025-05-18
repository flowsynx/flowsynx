namespace FlowSynx.Application.Localizations;

public interface ILocalization
{
    string Get(string key);
    string Get(string key, params object[] args);
}