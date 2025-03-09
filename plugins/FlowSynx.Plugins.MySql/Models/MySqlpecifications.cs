using FlowSynx.Abstractions.Attributes;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class MySqlpecifications : Specifications
{
    [RequiredMember]
    public string Url { get; set; } = string.Empty;
}