using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authentication.Models.Entity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace Authentication.Application.Service;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config) => _config = config;

    public Task<string> GenerateAccessToken(User user)
    {
        // Fail-safe validation to prevent generating a token with empty data
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrEmpty(user.TenantId)) 
            throw new InvalidOperationException("Cannot generate access token for a user without a valid TenantId.");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            
            // Securely embeds the tenant context into the cryptographically signed JWT payload
            new Claim("tenant_id", user.TenantId)
        };

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Secret Key is missing from configuration.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Fallback to 15 minutes if configuration key is missing
        var expirationMinutes = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes");
        if (expirationMinutes <= 0) expirationMinutes = 15; 

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        // Since there are no asynchronous await calls inside this method, 
        // we use Task.FromResult to cleanly satisfy the Task return signature without compiler overhead.
        return Task.FromResult(tokenString);
    }
}