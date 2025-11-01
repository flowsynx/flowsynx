namespace FlowSynx.Application.Secrets;

public interface ISecretProvider
{
    string Name { get; }

    Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(
        CancellationToken cancellationToken = default);
}