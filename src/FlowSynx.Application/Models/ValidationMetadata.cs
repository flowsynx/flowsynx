using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Application.Models;

public class ValidationMetadata
{
    public string Resource { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public DateTimeOffset ValidatedAt { get; set; }
}