using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rail.Data.Data;
using Rail.Data.Models;

namespace Rail.Authentication;

public class AuthService : IAuthService
{
    private readonly RailDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(RailDbContext context, IOptions<JwtSettings> jwtSettings, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByUsernameAsync(request.Username);

        if (user == null || !user.IsActive || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            return null;
        }

        var token = GenerateJwtToken(user);
        var expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

        _logger.LogInformation("Successful login for user: {Username}", request.Username);

        return new LoginResponse
        {
            Token = token,
            Expires = expires,
            Roles = user.GetRoles()
        };
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("sub", user.Id.ToString()),
            new("username", user.Username)
        };

        // Add role claims
        foreach (var role in user.GetRoles())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}