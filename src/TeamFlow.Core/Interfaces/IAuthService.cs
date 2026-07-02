using TeamFlow.Core.DTOs.Auth;
 
namespace TeamFlow.Core.Interfaces;
 
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
 