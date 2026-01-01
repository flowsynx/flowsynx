using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Infrastructure.Logging;

public sealed class TenantLoggingOptions
{
    public string TenantId { get; init; } = default!;
    public string? SeqUrl { get; init; }
    public string? FilePath { get; init; }
    public LogEventLevel MinimumLevel { get; init; } = LogEventLevel.Information;
}