using System.Security.Claims;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]                    // ← Protects entire controller
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UserController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            // Because of the Global Query Filter in ApplicationDbContext,
            // _userManager.Users automatically applies the WHERE TenantId = 'current_tenant' clause!
            var users = await _userManager.Users
                .Select(u => new 
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FullName,
                    u.TenantId
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving tenant users.", details = ex.Message });
        }
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.UserName,
            user.FullName
        });
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]   // Role-based authorization
    public IActionResult AdminOnly()
    {
        return Ok("Admin content");
    }
}