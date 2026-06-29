
using Authentication.Data;
using Authentication.Models.Entity;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

public record TokenRefreshResult(bool Success, string Message, string? NewAccessToken = null, string? NewRefreshToken = null);

public class RefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _config;

    public RefreshTokenService(ApplicationDbContext context, JwtService jwtService, IConfiguration config)
    {
        _context = context;
        _jwtService = jwtService;
        _config = config;
    }

    public async Task<(string RefreshToken, DateTime Expiry)> GenerateRefreshToken(User user)
{
    // 1. Generate a brand new cryptographically strong random token string
    var randomNumber = new byte[64];
    using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
    rng.GetBytes(randomNumber);
    
    var tokenString = Convert.ToBase64String(randomNumber);
    var expiryTime = DateTime.UtcNow.AddDays(7);

    // 2. Map all entity fields cleanly. 
    // Ensure you are assigning 'tokenString', NOT an uninitialized variable or property.
    var refreshTokenEntity = new RefreshToken
    {
        UserId = user.Id,
        TenantId = user.TenantId, 
        Token = tokenString, // <-- CRITICAL FIX: Ensure this is explicitly assigned here!
        JwtId = Guid.NewGuid().ToString(),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = expiryTime,
        IsRevoked = false
    };

    // 3. Save to database context
    await _context.RefreshTokens.AddAsync(refreshTokenEntity);
    
    // Line 44: This save changes call will no longer crash because Token contains data
    await _context.SaveChangesAsync(); 

    // 4. Return the tuple back to the AuthController
    return (tokenString, expiryTime); 
}


public async Task<TokenRefreshResult> RefreshTokenAsync(string token)
{
    // 1. Find the token in the database, including the associated user data
    var storedToken = await _context.RefreshTokens
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.Token == token);

    // 2. Validate token existence and security boundaries
    if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
    {
        return new TokenRefreshResult(Success: false, Message: "Invalid, expired, or revoked refresh token.");
    }

    // 3. Mark the old token as used/revoked (Token Rotation Pattern)
    storedToken.IsRevoked = true;
    _context.RefreshTokens.Update(storedToken);

    // 4. Generate a brand new access token and a brand new refresh token split
    var accessToken = await _jwtService.GenerateAccessToken(storedToken.User);
    var (newRefreshToken, _) = await GenerateRefreshToken(storedToken.User);

    return new TokenRefreshResult(
        Success: true, 
        Message: "Tokens rotated successfully.", 
        NewAccessToken: accessToken, 
        NewRefreshToken: newRefreshToken
    );
}
}


// public record TokenRefreshResult(
//     bool Success, 
//     string Message, 
//     string? NewAccessToken = null, 
//     string? NewRefreshToken = null
// );