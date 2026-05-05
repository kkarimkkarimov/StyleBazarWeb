using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Services.Authentication;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

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
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        IdentityResult result = await _authService.RegisterAsync(dto);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Registration successful." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        SignInResult result = await _authService.LoginAsync(dto);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(new { message = "Login successful." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok(new { message = "Logout successful." });
    }
}
