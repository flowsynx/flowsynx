using FlowSynx.Connectors.Storage.Memory.Models;

namespace FlowSynx.Connectors.Storage.Memory.Services;

public interface IMemoryMetricsClient
{
    MemoryMetrics GetMetrics();
}