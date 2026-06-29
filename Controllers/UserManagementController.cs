using Authentication.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/tenants/users")]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    // Helper property to extract current token's verified tenant context securely
    private string CurrentTenantId => User.FindFirst("tenant_id")?.Value 
        ?? throw new InvalidOperationException("Tenant identity claim missing from access token.");

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManagementService.GetAllUsersInTenantAsync(CurrentTenantId);
        return Ok(users);
    }

    [HttpGet("role/{roleName}")]
    public async Task<IActionResult> GetUsersByRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return BadRequest("Role name is required.");
        
        var users = await _userManagementService.GetUsersByRoleAsync(roleName.Trim(), CurrentTenantId);
        return Ok(users);
    }

    [HttpPost("add-role")]
    public async Task<IActionResult> AddRoleToUser([FromBody] UserRoleRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.RoleName))
        {
            return BadRequest("UserId and RoleName are required.");
        }

        var result = await _userManagementService.AddUserToRoleAsync(request.UserId, request.RoleName.Trim(), CurrentTenantId);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = $"Role '{request.RoleName}' successfully added to user." });
    }

    [HttpPost("remove-role")]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] UserRoleRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.RoleName))
        {
            return BadRequest("UserId and RoleName are required.");
        }

        var result = await _userManagementService.RemoveUserFromRoleAsync(request.UserId, request.RoleName.Trim(), CurrentTenantId);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = $"Role '{request.RoleName}' successfully removed from user." });
    }


    [HttpPut("update-role")]
        public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId) || 
                string.IsNullOrWhiteSpace(request.OldRoleName) || 
                string.IsNullOrWhiteSpace(request.NewRoleName))
            {
                return BadRequest("UserId, OldRoleName, and NewRoleName are required fields.");
            }

            var result = await _userManagementService.UpdateUserRoleAsync(
                request.UserId, 
                request.OldRoleName.Trim(), 
                request.NewRoleName.Trim(), 
                CurrentTenantId
            );

            if (!result.Succeeded) 
                return BadRequest(result.Errors);

            return Ok(new { message = $"User role updated successfully from '{request.OldRoleName}' to '{request.NewRoleName}'." });
        }

// Add this supporting record definition to your Dtos namespace
    public record UpdateUserRoleRequestDto(string UserId, string OldRoleName, string NewRoleName);

    
}