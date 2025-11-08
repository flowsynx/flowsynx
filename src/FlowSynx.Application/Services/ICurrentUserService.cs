namespace FlowSynx.Application.Services;

/// <summary>
/// Provides data about the authenticated principal associated with the current HTTP request.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Retrieves the unique identifier of the current user, or an empty string when not available.
    /// </summary>
    string UserId();

    /// <summary>
    /// Retrieves the display name of the current user, or an empty string when not available.
    /// </summary>
    string UserName();

    /// <summary>
    /// Indicates whether the current request is associated with an authenticated user.
    /// </summary>
    bool IsAuthenticated();

    /// <summary>
    /// Retrieves the role claims associated with the current user.
    /// </summary>
    List<string> Roles();

    /// <summary>
    /// Ensures the current request is authenticated and throws if no user context exists.
    /// </summary>
    void ValidateAuthentication();
}
