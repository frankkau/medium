using Authentication.Application.IServices;
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
private readonly SignInManager<User> _signInManager;
private readonly JwtService _jwtService;
private readonly RefreshTokenService _refreshTokenService;
private readonly RoleManager<ApplicationRole> _roleManager;
private readonly ApplicationDbContext _context;
private readonly ITenantService _tenantService; // Added service reference

public AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    JwtService jwtService,
    RefreshTokenService refreshTokenService,
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext context,
    ITenantService tenantService) // Inject service context
{
    _userManager = userManager;
    _signInManager = signInManager;
    _jwtService = jwtService;
    _refreshTokenService = refreshTokenService;
    _roleManager = roleManager;
    _context = context;
    _tenantService = tenantService;
}

[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return BadRequest("Email and password are required.");
    }

    // 1. Resolve active tenant boundary upfront (returns string?)
    var tenantIdFromService = _tenantService.GetCurrentTenantId();

    // 2. Validate and enforce non-nullability right here for compiler type safety
    if (string.IsNullOrWhiteSpace(tenantIdFromService))
    {
        return BadRequest("Unable to determine tenant context. Ensure the X-Tenant-Id header or subdomain is valid.");
    }

    // 3. Assign to a guaranteed non-nullable local variable
    string currentTenantId = tenantIdFromService;

    // Defensive Check: Prevent index issues by filtering with current tenant context
    var userExists = await _userManager.Users
        .AnyAsync(u => u.NormalizedEmail == request.Email.ToUpper().Trim() && u.TenantId == currentTenantId);

    if (userExists)
    {
        return BadRequest(new { message = "A user with this email already exists under this tenant." });
    }

    var user = new User 
    { 
        UserName = request.Email.Trim(), 
        Email = request.Email.Trim(),
        FullName = request.FullName,
        TenantId = currentTenantId // Warning CS8601: Completely resolved
    };

    var result = await _userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return BadRequest(result.Errors);
    }

    if (!string.IsNullOrWhiteSpace(request.Role))
    {
        var roleName = request.Role.Trim();
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        
        if (!roleExists)
        {
            var newRole = new ApplicationRole 
            { 
                Name = roleName,
                NormalizedName = roleName.ToUpper(), // Explicitly set normalized name to support our tenant indexes
                TenantId = currentTenantId // Warning CS8601: Completely resolved
            };
            
            var roleResult = await _roleManager.CreateAsync(newRole);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user); // Rollback user creation on role provision failure
                return BadRequest(roleResult.Errors);
            }
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!addRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(addRoleResult.Errors);
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
        // 1. Resolve tenant dynamically from the HTTP Request (Header or Subdomain)
        var requestTenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(requestTenantId))
        {
            return BadRequest(new { message = "Multi-tenant context execution missing. Provide an 'X-Tenant-Id' header or use an authorized tenant subdomain routing link." });
        }

        // 2. Locate user bypassing filters to verify existence
        var user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password authentication credentials." });
        }

        // 3. CRITICAL SECURITY CHECK: Ensure user matches resolved request tenant context
        if (user.TenantId != requestTenantId)
        {
            return Unauthorized(new { message = "Access denied. Your account is not registered to access this tenant domain portal." });
        }

        // 4. Validate credentials
        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!passwordCheck.Succeeded)
        {
            if (passwordCheck.IsLockedOut)
            {
                return StatusCode(423, new { message = "This account has been temporarily locked due to excessive failed attempts." });
            }
            return Unauthorized(new { message = "Invalid email or password authentication credentials." });
        }

        // 5. Build Token Packages
        var accessToken = await _jwtService.GenerateAccessToken(user);
        var (refreshToken, expiry) = await _refreshTokenService.GenerateRefreshToken(user);

        // Append HttpOnly Cookies securely
        SetAuthCookies(accessToken, refreshToken);

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
            var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(-1) // Force expiry in the past
            };

            Response.Cookies.Append("accessToken", "", cookieOptions);
            Response.Cookies.Append("refreshToken", "", cookieOptions);

            return Ok(new { message = "Logged out successfully" });
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