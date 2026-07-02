using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Auth;
using TeamFlow.Core.Interfaces;
 
namespace TeamFlow.API.Controllers;
 
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
 
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
 
    /// <summary>ثبت‌نام کاربر جدید</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), ApiResponse<AuthResponse>.Ok(result, "ثبت‌نام با موفقیت انجام شد."));
    }
 
    /// <summary>ورود به حساب کاربری</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "ورود موفقیت‌آمیز بود."));
    }
 
    /// <summary>دریافت توکن جدید با Refresh Token</summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "توکن با موفقیت تمدید شد."));
    }
 
    /// <summary>خروج و ابطال Refresh Token</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return Ok(ApiResponse.Ok("خروج با موفقیت انجام شد."));
    }
 
    /// <summary>تغییر رمز عبور</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);
 
        await _authService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse.Ok("رمز عبور با موفقیت تغییر یافت."));
    }
 
    /// <summary>دریافت اطلاعات کاربر جاری</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Me()
    {
        var user = new
        {
            Id = User.FindFirstValue("sub"),
            Email = User.FindFirstValue(ClaimTypes.Email),
            FirstName = User.FindFirstValue("firstName"),
            LastName = User.FindFirstValue("lastName"),
        };
 
        return Ok(ApiResponse<object>.Ok(user));
    }
}