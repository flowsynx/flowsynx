namespace FlowSynx.Application.Services;

public interface IConfigurationService
{
    Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> GetValue<T>(string key, T defaultValue = default, CancellationToken cancellationToken = default);
    Task UpdateAsync(string key, object value, string userId, CancellationToken cancellationToken = default);
}