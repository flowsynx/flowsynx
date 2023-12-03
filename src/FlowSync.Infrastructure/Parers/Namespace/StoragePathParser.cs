using EnsureThat;
using FlowSync.Abstractions;
using FlowSync.Core.Common;
using FlowSync.Core.Parers.Namespace;
using FlowSync.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSync.Infrastructure.Parers.Namespace;

internal class NamespaceParser : INamespaceParser
{
    private readonly ILogger<NamespaceParser> _logger;

    public NamespaceParser(ILogger<NamespaceParser> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public PluginNamespace Parse(string type)
    {
        try
        {
            var terms = type.Split("/");
            if (terms.Length != 2)
            {
                _logger.LogError($"The given type {type} is not valid!");
                throw new NamespaceParserException($"The given type {type} is not valid!");
            }

            var firstTerm = terms[0].Split(".");
            if (firstTerm.Length != 2)
            {
                _logger.LogError($"The given type {terms[0]} is not valid!");
                throw new NamespaceParserException($"The given type {terms[0]} is not valid!");
            }

            return EnumUtils.GetEnumValueOrDefault<PluginNamespace>(firstTerm[1])!.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new NamespaceParserException(ex.Message);
        }
    }

    public void Dispose() { }
}