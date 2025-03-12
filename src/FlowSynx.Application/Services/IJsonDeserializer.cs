using FlowSynx.Application.Models;

namespace FlowSynx.Application.Services;

public interface IJsonDeserializer
{
    T Deserialize<T>(string? input);
    T Deserialize<T>(string input, JsonSerializationConfiguration configuration);
}