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
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid token claims context." });
        }

        // 2. Fetch the user (Your DbContext global filter implicitly makes sure 
        // they can only be found if they belong to the current active tenant)
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found within this tenant scope." });
        }

        // 3. Retrieve the tenant-restricted roles mapped to this user
        var roles = await _userManager.GetRolesAsync(user);

        // 4. Return clean payload back to your Next.js application
        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.TenantId,
            Roles = roles // E.g., ["Admin"] or ["Teacher"]
        });
    
    }

   
}