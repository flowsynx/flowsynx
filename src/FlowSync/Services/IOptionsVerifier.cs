using FlowSync.Commands;

namespace FlowSync.Services;

public interface IOptionsVerifier
{
    void Verify(ref CommandOptions options);
}