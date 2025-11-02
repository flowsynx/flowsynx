namespace FlowSynx.Application.Serialization;

public interface IJsonSanitizer
{
    string Sanitize(string json);
}