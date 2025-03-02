using FlowSynx.Core.Models;

namespace FlowSynx.Core.Services;

public interface IJsonSerializer
{
    string Serialize(object? input);
    string Serialize(object? input, JsonSerializationConfiguration configuration);
}