using FlowSynx.Application.Core.Serializations;
using FlowSynx.Infrastructure.Serializations.Json.Exceptions;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Serializations.Json;

public class JsonDeserializer : IDeserializer
{
    private readonly JsonSerializerOptions _options;

    public JsonDeserializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        _options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    public T Deserialize<T>(string? input) where T : class
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new JsonSerializationInputRequiredException(typeof(T));
        }

        try
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(input, _options);
            if (result is null)
            {
                throw new JsonSerializationException($"Deserialization returned null for type {typeof(T).Name}.");
            }
            return result;
        }
        catch (JsonException ex)
        {
            throw new JsonSerializationException($"Failed to parse JSON as {typeof(T).Name}", ex);
        }
    }

    public Task<object> DeserializeDynamicAsync(string json)
    {
        try
        {
            var element = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json, _options);
            return Task.FromResult<object>(element);
        }
        catch (JsonException ex)
        {
            throw new JsonSerializationException("Failed to parse JSON", ex);
        }
    }

    public Task<bool> TryDeserializeAsync<T>(string json, out T result) where T : class
    {
        T? tempResult = null;
        try
        {
            tempResult = System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
            result = tempResult!;
            return Task.FromResult(result != null);
        }
        catch
        {
            result = null!;
            return Task.FromResult(false);
        }
    }
}
