namespace Authentication.Application.Service;

using Authentication.Application.IServices;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleService(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<ApplicationRole>> GetAllRolesAsync(string tenantId)
    {
        return await _roleManager.Roles
            .Where(r => r.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<ApplicationRole?> GetRoleByIdAsync(string id, string tenantId)
    {
        return await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
    }

    public async Task<IdentityResult> CreateRoleAsync(string roleName, string tenantId)
    {
        // Prevent duplicate role names within the same tenant context
        var roleExists = await _roleManager.Roles
            .AnyAsync(r => r.Name == roleName && r.TenantId == tenantId);

        if (roleExists)
        {
            return IdentityResult.Failed(new IdentityError 
            { 
                Code = "DuplicateRole", 
                Description = $"Role '{roleName}' already exists within this tenant." 
            });
        }

        var role = new ApplicationRole
        {
            Name = roleName,
            TenantId = tenantId
        };

        return await _roleManager.CreateAsync(role);
    }

    public async Task<IdentityResult> UpdateRoleAsync(string id, string newRoleName, string tenantId)
    {
        var role = await GetRoleByIdAsync(id, tenantId);
        if (role == null)
        {
            return IdentityResult.Failed(new IdentityError { Code = "NotFound", Description = "Role not found in your tenant context." });
        }

        // FIX: Using SetRoleNameAsync automatically triggers Identity's Internal Name Normalizer 
        // to re-evaluate NormalizedName (e.g., converting "Teacher" to "TEACHER").
        var nameResult = await _roleManager.SetRoleNameAsync(role, newRoleName);
        if (!nameResult.Succeeded)
        {
            return nameResult;
        }

        return await _roleManager.UpdateAsync(role);
    }

    public async Task<IdentityResult> DeleteRoleAsync(string id, string tenantId)
    {
        var role = await GetRoleByIdAsync(id, tenantId);
        if (role == null)
        {
            return IdentityResult.Failed(new IdentityError { Code = "NotFound", Description = "Role not found in your tenant context." });
        }

        return await _roleManager.DeleteAsync(role);
    }
}