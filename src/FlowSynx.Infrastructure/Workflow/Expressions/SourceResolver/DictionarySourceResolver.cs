using FlowSynx.Domain;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Infrastructure.Workflow.Expressions.SourceResolver;

internal class DictionarySourceResolver : ISourceResolver
{
    private readonly Dictionary<string, object?> _dictionary;
    private readonly string _sourceName;

    public DictionarySourceResolver(Dictionary<string, object?> dictionary, string sourceName)
    {
        _dictionary = dictionary;
        _sourceName = sourceName;
    }

    public Task<object?> ResolveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_dictionary.TryGetValue(key, out var value))
            throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound,
                $"ExpressionParser: {_sourceName}('{key}') not found");

        return Task.FromResult(value);
    }
}