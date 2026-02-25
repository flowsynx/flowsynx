using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;

namespace FlowSynx.Application.Core.Services;

public interface IActivityCompatibilityService
{
    bool IsCompatible(Activity activity, RuntimeEnvironment env, out List<string> issues);
}