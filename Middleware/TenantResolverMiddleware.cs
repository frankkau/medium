namespace Authentication.Application;

using System.IdentityModel.Tokens.Jwt;
using Authentication.Application.IServices;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var host = context.Request.Host.Host;
        var hostParts = host.Split('.');
        string? resolvedSubdomain = null;

        // 1. Extract Subdomain from Host
        if (hostParts.Length > 2)
        {
            var subdomain = hostParts[0].ToLower();
            if (subdomain != "www" && subdomain != "api")
            {
                resolvedSubdomain = subdomain;
            }
        }

        // Fallback for local development / Postman via header
        if (string.IsNullOrEmpty(resolvedSubdomain) &&
            context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerId))
        {
            resolvedSubdomain = headerId.ToString().ToLower().Trim();
        }

        // If no tenant context can be resolved, halt the request
        if (string.IsNullOrEmpty(resolvedSubdomain))
        {
            // Allow global endpoints (e.g. landing page, master tenant registration)
            if (context.Request.Path.StartsWithSegments("/api/global"))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Multi-tenant routing context missing. A valid subdomain is required."
            });
            return;
        }

        // 2. Cross-Verify Tenant Isolation with JWT Token (Bearer header OR cookie)
        var tokenString = string.Empty;

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // Bearer token from Authorization header (Scalar UI / Postman)
            tokenString = authHeader.Substring("Bearer ".Length).Trim();
        }
        else if (context.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
        {
            // Cookie-based JWT (standard browser client flow)
            tokenString = cookieToken;
        }

        if (!string.IsNullOrEmpty(tokenString))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();

                if (handler.CanReadToken(tokenString))
                {
                    var jwtToken = handler.ReadJwtToken(tokenString);
                    var tokenTenantClaim = jwtToken.Claims
                        .FirstOrDefault(c => c.Type == "tenant_id")?.Value;

                    // Block cross-tenant token usage
                    if (!string.IsNullOrEmpty(tokenTenantClaim) &&
                        !tokenTenantClaim.Equals(resolvedSubdomain, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "Cross-tenant access violation. Token does not match the current tenant environment."
                        });
                        return;
                    }
                }
            }
            catch
            {
                // Malformed tokens are handled downstream by the Authentication middleware
            }
        }

        // 3. Commit verified tenant to the scoped service for this request
        tenantService.SetTenant(resolvedSubdomain);

        await _next(context);
    }
}