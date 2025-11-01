using FlowSynx.Application.Secrets;

namespace FlowSynx.Infrastructure.Secrets;

public interface ISecretFactory
{
    ISecretProvider? GetDefaultProvider();
}