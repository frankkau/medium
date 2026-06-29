

using Authentication.Application.IServices;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserManagementService(
        UserManager<User> userManager, 
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersInTenantAsync(string tenantId)
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserResponseDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserResponseDto(user.Id, user.Email!, user.FullName, roles));
        }

        return userDtos;
    }

    public async Task<IEnumerable<UserResponseDto>> GetUsersByRoleAsync(string roleName, string tenantId)
    {
        // 1. Double-check that the role actually belongs to this tenant context
        var roleExistsInTenant = await _roleManager.Roles
            .AnyAsync(r => r.Name == roleName && r.TenantId == tenantId);

        if (!roleExistsInTenant)
        {
            return Enumerable.Empty<UserResponseDto>();
        }

        // 2. Identity framework handles the filtering under the hood combined with our query filters
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        var userDtos = new List<UserResponseDto>();

        foreach (var user in usersInRole)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserResponseDto(user.Id, user.Email!, user.FullName, roles));
        }

        return userDtos;
    }

    public async Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName, string tenantId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found within this tenant context." });
        }

        // Validate that the target role belongs to the current tenant boundary
        var roleExists = await _roleManager.Roles.AnyAsync(r => r.Name == roleName && r.TenantId == tenantId);
        if (!roleExists)
        {
            return IdentityResult.Failed(new IdentityError { Code = "RoleNotFound", Description = $"The role '{roleName}' is not provisioned for this tenant." });
        }

        return await _userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName, string tenantId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found within this tenant context." });
        }

        return await _userManager.RemoveFromRoleAsync(user, roleName);
    }

    public async Task<IdentityResult> UpdateUserRoleAsync(string userId, string oldRoleName, string newRoleName, string tenantId)
{
    // 1. Locate user under active tenant context filter
    var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
    {
        return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found in this tenant boundary." });
    }

    // 2. Defensive Check: Ensure the target new role exists for this tenant
    var newRoleExists = await _roleManager.Roles.AnyAsync(r => r.Name == newRoleName && r.TenantId == tenantId);
    if (!newRoleExists)
    {
        return IdentityResult.Failed(new IdentityError { Code = "RoleNotFound", Description = $"The target role '{newRoleName}' is not provisioned for this tenant." });
    }

    // 3. Remove the old role mapping
    var removeResult = await _userManager.RemoveFromRoleAsync(user, oldRoleName);
    if (!removeResult.Succeeded)
    {
        return removeResult;
    }

    // 4. Assign the new role mapping
    var addResult = await _userManager.AddToRoleAsync(user, newRoleName);
    if (!addResult.Succeeded)
    {
        // Fail-safe rollback: Re-assign the old role if adding the new one breaks
        await _userManager.AddToRoleAsync(user, oldRoleName);
        return addResult;
    }

    return IdentityResult.Success;
}
}
