using Microsoft.Extensions.Options;
using Moq;
using TeamFlow.Core.DTOs.Auth;
using TeamFlow.Core.Entities;
using TeamFlow.Infrastructure.Services;
using TeamFlow.Tests.Helpers;
using Xunit;
using System.Threading.Tasks;

namespace TeamFlow.Tests.Auth;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var context = TestDbContextFactory.Create();
        var userManager = TestDbContextFactory.CreateUserManager(context);
        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "super_secret_key_for_testing_minimum_32_chars!!",
            Issuer = "TeamFlow",
            Audience = "TeamFlowUsers",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        });

        _authService = new AuthService(userManager, context, jwtSettings);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsAuthResponse()
    {
        var request = new RegisterRequest("علی", "رضایی", "ali@test.com", "Test@123");

        var result = await _authService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("ali@test.com", result.User.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsException()
    {
        var request = new RegisterRequest("علی", "رضایی", "duplicate@test.com", "Test@123");
        await _authService.RegisterAsync(request);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        var registerRequest = new RegisterRequest("سارا", "محمدی", "sara@test.com", "Test@123");
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest("sara@test.com", "Test@123");
        var result = await _authService.LoginAsync(loginRequest);

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        var registerRequest = new RegisterRequest("رضا", "کریمی", "reza@test.com", "Test@123");
        await _authService.RegisterAsync(registerRequest);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest("reza@test.com", "WrongPass")));
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // یه context و authService جداگانه برای هر مرحله
        var context = TestDbContextFactory.Create();
        var userManager = TestDbContextFactory.CreateUserManager(context);
        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "super_secret_key_for_testing_minimum_32_chars!!",
            Issuer = "TeamFlow",
            Audience = "TeamFlowUsers",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        });

        var authService = new AuthService(userManager, context, jwtSettings);

        // Register
        var request = new RegisterRequest("مهسا", "احمدی", "mahsa2@test.com", "Test@123");
        var auth = await authService.RegisterAsync(request);

        // Refresh با همون context
        var result = await authService.RefreshTokenAsync(auth.RefreshToken);

        Assert.NotNull(result);
        Assert.NotEqual(auth.RefreshToken, result.RefreshToken);
        Assert.NotEqual(auth.AccessToken, result.AccessToken);
    }
    [Fact]
    public async Task RefreshToken_WithInvalidToken_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync("invalid_token_xyz"));
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrent_Succeeds()
    {
        var request = new RegisterRequest("امیر", "حسینی", "amir@test.com", "OldPass@123");
        var auth = await _authService.RegisterAsync(request);

        var changeRequest = new ChangePasswordRequest("OldPass@123", "NewPass@456");
        await _authService.ChangePasswordAsync(auth.User.Id, changeRequest);

        var loginResult = await _authService.LoginAsync(new LoginRequest("amir@test.com", "NewPass@456"));
        Assert.NotNull(loginResult);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrent_ThrowsException()
    {
        var request = new RegisterRequest("فاطمه", "زارعی", "fateme@test.com", "OldPass@123");
        var auth = await _authService.RegisterAsync(request);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.ChangePasswordAsync(auth.User.Id,
                new ChangePasswordRequest("WrongPass", "NewPass@456")));
    }
}
