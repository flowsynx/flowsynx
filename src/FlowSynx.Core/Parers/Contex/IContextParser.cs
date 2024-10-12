using FlowSynx.Parsers;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Parers.Contex;

public interface IContextParser : IParser
{
    Context Parse(string path);
}