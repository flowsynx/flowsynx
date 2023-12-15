using System.Collections;

namespace FlowSync.Core.Common.Services;

public interface IEnvironmentVariablesManager
{
    string? Get(string variableName);
    void Set(string variableName, string value);
}