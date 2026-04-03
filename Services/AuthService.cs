using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using InstagramClone.Api.Models;
using InstagramClone.Api.Data;
using InstagramClone.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace InstagramClone.Api.Services;

public class AuthService
{
    private readonly InstagramContext _db;
    private readonly JwtRuntimeSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(InstagramContext db, JwtRuntimeSettings jwtSettings, ILogger<AuthService> logger)
    {
        _db = db;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(string email, string username, string password, string role = "Consumer")
    {
        try
        {
            var normalizedRole = role.Equals("Creator", StringComparison.OrdinalIgnoreCase)
                ? "Creator"
                : "Consumer";

            // Validation
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return new AuthResponse { Success = false, Message = "Email already exists" };

            if (await _db.Users.AnyAsync(u => u.Username == username))
                return new AuthResponse { Success = false, Message = "Username already exists" };

            if (password.Length < 8)
                return new AuthResponse { Success = false, Message = "Password must be at least 8 characters" };

            // Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = username,
                PasswordHash = hashedPassword,
                Role = normalizedRole,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"User registered: {email}");

            // Generate token
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Token = token,
                ExpiresIn = 86400, // 24 hours
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Registration error: {ex.Message}");
            return new AuthResponse { Success = false, Message = "Registration failed" };
        }
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return new AuthResponse { Success = false, Message = "Invalid email or password" };

            var token = GenerateJwtToken(user);

            _logger.LogInformation($"User logged in: {email}");

            return new AuthResponse
            {
                Success = true,
                Token = token,
                ExpiresIn = 86400,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login error: {ex.Message}");
            return new AuthResponse { Success = false, Message = "Login failed" };
        }
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
