

using Authentication.Models.Dtos;
using Microsoft.AspNetCore.Identity;

namespace Authentication.Application.IServices;

public interface IUserManagementService
{
    Task<IEnumerable<UserResponseDto>> GetUsersByRoleAsync(string roleName, string tenantId);
    Task<IEnumerable<UserResponseDto>> GetAllUsersInTenantAsync(string tenantId);
    Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName, string tenantId);
    Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName, string tenantId);
    Task<IdentityResult> UpdateUserRoleAsync(string userId, string oldRoleName, string newRoleName, string tenantId);
}

// Data Transfer Objects for clean API responses
public record UserResponseDto(string Id, string Email, string FullName, IEnumerable<string> Roles);
public record UserRoleRequestDto(string UserId, string RoleName);
