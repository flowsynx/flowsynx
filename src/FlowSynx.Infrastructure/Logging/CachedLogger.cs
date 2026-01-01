using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public sealed record CachedLogger(
    ILogger Logger,
    DateTime CreatedAt);