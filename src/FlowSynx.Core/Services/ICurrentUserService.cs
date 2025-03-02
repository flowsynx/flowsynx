namespace FlowSynx.Core.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
    List<string> Roles { get; }
}