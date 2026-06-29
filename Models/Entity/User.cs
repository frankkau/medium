using Authentication.Application.IServices;
using Microsoft.AspNetCore.Identity;

namespace Authentication.Models.Entity;

public class User : IdentityUser, IMustHaveTenant
{
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public string FullName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    // Navigation property for the 1:1 relationship
    public virtual StudentProfile? StudentProfile { get; set; }

    
}
