namespace Authentication.Application.IServices;

using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;

public interface IRoleService
{
    Task<IEnumerable<ApplicationRole>> GetAllRolesAsync(string tenantId);
    Task<ApplicationRole?> GetRoleByIdAsync(string id, string tenantId);
    Task<IdentityResult> CreateRoleAsync(string roleName, string tenantId);
    Task<IdentityResult> UpdateRoleAsync(string id, string newRoleName, string tenantId);
    Task<IdentityResult> DeleteRoleAsync(string id, string tenantId);
}