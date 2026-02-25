using FlowSynx.Application.Models;

namespace FlowSynx.Application.Core.Services;

public interface IRuntimeEnvironmentProvider
{
    RuntimeEnvironment GetCurrent();
}