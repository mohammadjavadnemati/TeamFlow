using System.ComponentModel.DataAnnotations;
 
namespace TeamFlow.Core.DTOs.Auth;
 
public record RegisterRequest(
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 6)] string Password
);
 
public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
 
public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StringLength(100, MinimumLength = 6)] string NewPassword
);
 
public record RefreshTokenRequest(
    [Required] string RefreshToken
);
 
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    UserDto User
);
 
public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl
);