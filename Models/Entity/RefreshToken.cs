using Authentication.Application.IServices;

namespace Authentication.Models.Entity;

public class RefreshToken: IMustHaveTenant
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;       // Plain token (not stored in DB)
    public string TokenHash { get; set; } = string.Empty;   // Hashed
    public string JwtId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string UserId { get; set; }
    public User User { get; set; } = null!;
}
