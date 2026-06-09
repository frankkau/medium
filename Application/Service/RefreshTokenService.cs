
using Authentication.Data;
using Authentication.Models.Entity;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

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
        // Revoke previous tokens
        var oldTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in oldTokens)
            token.IsRevoked = true;

        var plainToken = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();
        var hashedToken = BCrypt.Net.BCrypt.HashPassword(plainToken);

        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = hashedToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTokenExpirationDays")),
            JwtId = Guid.NewGuid().ToString()
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return (plainToken, refreshTokenEntity.ExpiresAt);
    }

    public async Task<(bool Success, string? NewAccessToken, string? NewRefreshToken)> RefreshTokenAsync(string incomingRefreshToken)
    {
        var refreshTokenEntity = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

        if (refreshTokenEntity == null || 
            !BCrypt.Net.BCrypt.Verify(incomingRefreshToken, refreshTokenEntity.TokenHash))
            return (false, null, null);

        // Revoke current token
        refreshTokenEntity.IsRevoked = true;

        var newAccessToken = await _jwtService.GenerateAccessToken(refreshTokenEntity.User);
        var (newRefreshToken, _) = await GenerateRefreshToken(refreshTokenEntity.User);

        await _context.SaveChangesAsync();

        return (true, newAccessToken, newRefreshToken);
    }
}