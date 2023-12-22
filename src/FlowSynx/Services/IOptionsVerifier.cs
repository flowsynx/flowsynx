using FlowSynx.Commands;

namespace FlowSynx.Services;

public interface IOptionsVerifier
{
    void Verify(ref RootCommandOptions options);
}