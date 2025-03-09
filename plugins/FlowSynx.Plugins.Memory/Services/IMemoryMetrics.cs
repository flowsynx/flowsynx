using FlowSynx.Connectors.Storage.Memory.Models;

namespace FlowSynx.Connectors.Storage.Memory.Services;

public interface IMemoryMetrics
{
    Models.MemoryMetrics GetMetrics();
}