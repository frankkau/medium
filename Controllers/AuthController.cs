using Authentication.Application.Service;
using Authentication.Data;
using Authentication.Models.Dtos;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly JwtService _jwtService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public AuthController(UserManager<User> userManager, JwtService jwtService, 
                         RefreshTokenService refreshTokenService, RoleManager<ApplicationRole> roleManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _roleManager = roleManager;
        _context = context;

    }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1. Instantiate the user. 
            // Note: Do NOT manually assign TenantId here. 
            // Your DbContext's ApplyTenantTracking() will automatically inject it on SaveChanges.
            var user = new User 
            { 
                UserName = request.Email, 
                Email = request.Email,
                FullName = request.FullName
            };

            // 2. Create the user using UserManager
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // 3. Handle Role Assignment if a role is requested
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                // Because of the Global Query Filter we set up in DbContext, 
                // RoleManager will ONLY look for this role inside the current tenant.
                var roleExists = await _roleManager.RoleExistsAsync(request.Role);
                
                if (!roleExists)
                {
                    // Optional: Automatically create the role for the tenant if it doesn't exist,
                    // or return a bad request depending on your business logic.
                    var newRole = new ApplicationRole { Name = request.Role };
                    await _roleManager.CreateAsync(newRole);
                }

                // Assign the tenant-scoped role to the tenant-scoped user
                var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
                
                if (!roleResult.Succeeded)
                {
                    // If role assignment fails, you might want to handle cleanup or log it
                    return BadRequest(roleResult.Errors);
                }
            }

            return Ok(new AuthResponse("User registered successfully"));
        }

        [HttpGet("users-with-roles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var usersWithRoles = await _userManager.Users
                .Select(user => new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    // Join over the Identity mapping tables
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == user.Id)
                        .Join(_context.Roles, 
                            ur => ur.RoleId, 
                            role => role.Id, 
                            (ur, role) => role.Name)
                        .ToList()
                })
                .ToListAsync();

            return Ok(usersWithRoles);
        }
 

        [HttpGet("by-role/{roleName}")]
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            // Scopes automatically to the current tenant out-of-the-box
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            
            var response = users.Select(u => new { u.Id, u.Email, u.FullName });
            return Ok(response);
        }
        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetUserProfile(string userId)
        {
            // The query filter ensures a tenant can only look up their own users
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found in this tenant.");
            }

            // Identity looks up the user's roles matching the tenant constraint
            var roles = await _userManager.GetRolesAsync(user);

            var response = new 
            {
                user.Id,
                user.Email,
                user.FullName,
                Roles = roles // Returns a list of strings: ["Admin", "Teacher"]
            };

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return BadRequest(new AuthResponse(Message: "Invalid email or password", Success: false));

            var accessToken = await _jwtService.GenerateAccessToken(user);
            var (refreshToken, _) = await _refreshTokenService.GenerateRefreshToken(user);

            // Set HttpOnly Cookies (for production frontend)
            SetAuthCookies(accessToken, refreshToken);

            // Return tokens in response for testing / Postman / Swagger
            return Ok(new AuthResponse(
                Message: "Login successful",
                AccessToken: accessToken,
                RefreshToken: refreshToken
            ));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
                return Unauthorized(new AuthResponse("Refresh token missing", Success: false));

            var result = await _refreshTokenService.RefreshTokenAsync(refreshToken);

            if (!result.Success || result.NewAccessToken == null || result.NewRefreshToken == null)
                return Unauthorized(new AuthResponse("Invalid or expired refresh token", Success: false));

            SetAuthCookies(result.NewAccessToken, result.NewRefreshToken);

            return Ok(new AuthResponse(
                Message: "Tokens refreshed successfully",
                AccessToken: result.NewAccessToken,
                RefreshToken: result.NewRefreshToken
            ));
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok(new AuthResponse("Logged out successfully"));
        }

        private void SetAuthCookies(string accessToken, string refreshToken)
    {
        // Check if the current environment is NOT development
        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

        var accessOptions = new CookieOptions
        {
            HttpOnly = true,
            // Only set Secure to true in production/HTTPS
            Secure = isProduction, 
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("accessToken", accessToken, accessOptions);
        Response.Cookies.Append("refreshToken", refreshToken, refreshOptions);
    }
}