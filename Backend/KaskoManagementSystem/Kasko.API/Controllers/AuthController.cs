using Kasko.Business.DTOs.Auth;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        LoginDto dto)
    {
        var result =
            await _authService
                .LoginAsync(dto);

        return Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterDto dto)
    {
        var customerId =
            await _authService
                .RegisterAsync(dto);

        return Ok(new
        {
            customerId
        });
    }

    [HttpPost("forgot-password/send")]
    [AllowAnonymous]
    public async Task<IActionResult> SendResetCode(PasswordResetRequestDto dto)
    {
        return Ok(await _authService.SendPasswordResetCodeAsync(dto));
    }

    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(PasswordResetConfirmDto dto)
    {
        await _authService.ResetPasswordAsync(dto);

        return Ok(new { message = "Şifreniz güncellendi. Yeni şifrenizle giriş yapabilirsiniz." });
    }
}
