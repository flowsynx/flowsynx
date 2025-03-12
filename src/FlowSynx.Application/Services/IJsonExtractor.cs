using FlowSynx.Application.Models;

namespace FlowSynx.Application.Services;

public interface IJsonExtractor
{
    string ExtractorObject(string json, string key);
    string ExtractorArray(string json, string key);
}