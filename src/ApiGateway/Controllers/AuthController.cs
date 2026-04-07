
using Microsoft.AspNetCore.Mvc;
using Shared.Auth;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        // Simple token generation for demo purposes
        // In production, this would validate against a user store
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new { Error = "Email and Role are required" });
        }

        var validRoles = new[] { "Admin", "User" };
        if (!validRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { Error = "Role must be 'Admin' or 'User'" });
        }

        var userId = request.UserId ?? Guid.NewGuid().ToString();
        var token = JwtHelper.GenerateToken(userId, request.Role, request.Email);

        _logger.LogInformation("Token generated for {Email} with role {Role}", request.Email, request.Role);

        return Ok(new
        {
            Token = token,
            UserId = userId,
            Role = request.Role,
            ExpiresIn = "2 hours"
        });
    }
}

public record TokenRequest(string Email, string Role, string? UserId = null);
