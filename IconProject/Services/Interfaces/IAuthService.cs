using IconProject.Common.Dtos;
using IconProject.Common.Dtos.Requests.Auth;
using IconProject.Common.Dtos.Responses.Auth;

namespace IconProject.Services.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
