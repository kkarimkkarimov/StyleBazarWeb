using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Services.Authentication;

namespace Stylebazar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        RegisterResultDto result = await _authService.RegisterAsync(dto, ct);
        if (!result.Succeeded)
        {
            // Same shape as ValidationException emits for the rest of the API.
            return BadRequest(new
            {
                errors = result.FieldErrors,
                error = result.GeneralError
            });
        }

        return Ok(new { message = "Registration successful.", userId = result.UserId });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginDto dto)
    {
        LoginResultDto result = await _authService.LoginAsync(dto);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok(new { message = "Logout successful." });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            userName = User.FindFirstValue(ClaimTypes.Name),
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
        });
    }
}
