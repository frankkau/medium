using Authentication.Application.IServices;
using Microsoft.AspNetCore.Identity;

namespace Authentication.Models.Entity;
 
public class ApplicationRole : IdentityRole, IMustHaveTenant
{
    public string TenantId { get; set; } = null!;
}