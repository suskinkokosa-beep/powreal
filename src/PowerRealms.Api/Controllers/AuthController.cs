using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILocalizationService _localization;

    public AuthController(IAuthService auth, ILocalizationService localization)
    {
        _auth = auth;
        _localization = localization;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var user = await _auth.RegisterAsync(dto.Username, dto.Password, dto.Role);
            return Ok(new { 
                message = _localization.GetMessage("Auth.RegisterSuccess"),
                user = new { user.Id, user.Username, user.Role }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = _localization.GetMessage("Auth.UserExists"), error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _auth.AuthenticateAsync(dto.Username, dto.Password);
        if (token == null)
            return Unauthorized(new { message = _localization.GetMessage("Auth.LoginFailed") });
        
        return Ok(new { 
            message = _localization.GetMessage("Auth.LoginSuccess"),
            token 
        });
    }

    [HttpGet("languages")]
    public IActionResult GetLanguages()
    {
        return Ok(new
        {
            current = _localization.CurrentCulture,
            supported = _localization.SupportedCultures
        });
    }
}

public record RegisterDto(string Username, string Password, UserRole Role = UserRole.Member);
public record LoginDto(string Username, string Password);
