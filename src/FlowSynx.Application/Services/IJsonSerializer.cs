using FlowSynx.Application.Models;

namespace FlowSynx.Application.Services;

public interface IJsonSerializer
{
    string Serialize(object? input);
    string Serialize(object? input, JsonSerializationConfiguration configuration);
}