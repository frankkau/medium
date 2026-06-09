using Authentication.Application.Service;
using Authentication.Models.Dtos;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly JwtService _jwtService;
    private readonly RefreshTokenService _refreshTokenService;

    public AuthController(UserManager<User> userManager, JwtService jwtService, 
                         RefreshTokenService refreshTokenService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new User 
        { 
            UserName = request.Email, 
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        return result.Succeeded 
            ? Ok(new AuthResponse("User registered successfully")) 
            : BadRequest(result.Errors);
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