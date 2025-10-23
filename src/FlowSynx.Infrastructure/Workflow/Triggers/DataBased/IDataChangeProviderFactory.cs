namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Resolves <see cref="IDataChangeProvider"/> instances for configured provider keys.
/// </summary>
public interface IDataChangeProviderFactory
{
    IDataChangeProvider Resolve(string providerKey);
}
