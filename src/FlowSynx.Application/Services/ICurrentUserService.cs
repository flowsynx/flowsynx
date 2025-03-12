namespace FlowSynx.Application.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
    List<string> Roles { get; }
}