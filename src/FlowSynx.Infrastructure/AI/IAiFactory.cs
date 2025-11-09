using FlowSynx.Application.AI;

namespace FlowSynx.Infrastructure.AI;

public interface IAiFactory
{
    IAiProvider GetDefaultProvider();
}