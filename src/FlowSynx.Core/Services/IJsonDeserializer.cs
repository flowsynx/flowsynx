using FlowSynx.Core.Models;

namespace FlowSynx.Core.Services;

public interface IJsonDeserializer
{
    T Deserialize<T>(string? input);
    T Deserialize<T>(string input, JsonSerializationConfiguration configuration);
}