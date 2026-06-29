using Authentication.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Authentication.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/tenants/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    // Helper property to extract current token's verified tenant context securely
    private string CurrentTenantId => User.FindFirst("tenant_id")?.Value 
        ?? throw new InvalidOperationException("Tenant identity claim missing from access token.");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleService.GetAllRolesAsync(CurrentTenantId);
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var role = await _roleService.GetRoleByIdAsync(id, CurrentTenantId);
        if (role == null) return NotFound(new { message = "Role not found in your tenant context." });
        return Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Role name is required.");

        var result = await _roleService.CreateRoleAsync(request.Name, CurrentTenantId);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return StatusCode(201, new { message = "Role successfully provisioned." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("New role name is required.");

        var result = await _roleService.UpdateRoleAsync(id, request.Name, CurrentTenantId);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = "Role successfully updated." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _roleService.DeleteRoleAsync(id, CurrentTenantId);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = "Role removed successfully." });
    }
}

// Request Data Transfer Objects (DTOs)
public record CreateRoleRequest(string Name);
public record UpdateRoleRequest(string Name);