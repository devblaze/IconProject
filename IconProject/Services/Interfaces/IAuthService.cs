using IconProject.Dtos;
using IconProject.Dtos.Auth;

namespace IconProject.Services.Interfaces;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with token on success.</returns>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with token on success.</returns>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's information.
    /// </summary>
    /// <param name="userId">The user ID from the JWT token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User information on success.</returns>
    Task<Result<UserInfo>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
