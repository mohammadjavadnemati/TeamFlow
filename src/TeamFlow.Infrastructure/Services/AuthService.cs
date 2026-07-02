using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamFlow.Core.DTOs.Auth;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("این ایمیل قبلاً ثبت شده است.");

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new InvalidOperationException(string.Join(", ", errors));
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("ایمیل یا رمز عبور اشتباه است.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new UnauthorizedAccessException("ایمیل یا رمز عبور اشتباه است.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token is null || !token.IsActive)
            throw new UnauthorizedAccessException("Refresh token نامعتبر یا منقضی شده است.");

        // Rotate refresh token
        var newRefreshToken = GenerateRefreshToken();
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = newRefreshToken.Token;
        token.RevokedReason = "Replaced by new token";

        token.User.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var accessToken = GenerateAccessToken(token.User);
        return BuildAuthResponse(accessToken, newRefreshToken, token.User);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token is null || !token.IsActive)
            throw new UnauthorizedAccessException("Refresh token نامعتبر است.");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedReason = "Revoked by user";

        await _context.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new KeyNotFoundException("کاربر یافت نشد.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new InvalidOperationException(string.Join(", ", errors));
        }

        // Revoke all existing refresh tokens on password change
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "Password changed";
        }

        await _context.SaveChangesAsync();
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    // private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    // {
    //     var accessToken = GenerateAccessToken(user);
    //     var refreshToken = GenerateRefreshToken();

    //     user.RefreshTokens.Add(refreshToken);
    //     await _context.SaveChangesAsync();

    //     return BuildAuthResponse(accessToken, refreshToken, user);
    // }
    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        refreshToken.UserId = user.Id;

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return BuildAuthResponse(accessToken, refreshToken, user);
    }

    private string GenerateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken() => new()
    {
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
    };

    private AuthResponse BuildAuthResponse(string accessToken, RefreshToken refreshToken, ApplicationUser user) => new(
        AccessToken: accessToken,
        RefreshToken: refreshToken.Token,
        AccessTokenExpiry: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
        User: new UserDto(user.Id, user.FirstName, user.LastName, user.Email!, user.AvatarUrl)
    );
}