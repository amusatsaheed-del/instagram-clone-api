using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InstagramClone.Api.Services;
using InstagramClone.Api.DTOs;

namespace InstagramClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, IConfiguration config, ILogger<AuthController> logger)
    {
        _authService = authService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(request.Email, request.Username, request.Password, "Consumer");

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>
    /// Internal endpoint for creator enrollment. Not for public UI usage.
    /// </summary>
    [HttpPost("internal/create-creator")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCreator([FromBody] RegisterRequest request)
    {
        var configuredKey = _config["InternalAdmin:Key"];
        var providedKey = Request.Headers["X-Internal-Admin-Key"].ToString();

        if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey != providedKey)
        {
            return Unauthorized(new AuthResponse { Success = false, Message = "Unauthorized internal enrollment request" });
        }

        var result = await _authService.RegisterAsync(request.Email, request.Username, request.Password, "Creator");

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("Internal creator account created for {email}", request.Email);
        return CreatedAtAction(nameof(CreateCreator), result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    /// <summary>
    /// Validate current JWT token
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        return Ok(new { valid = true, timestamp = DateTime.UtcNow });
    }
}
