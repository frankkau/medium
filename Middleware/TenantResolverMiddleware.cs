namespace Authentication.Application;

using System.IdentityModel.Tokens.Jwt;
using Authentication.Application.IServices;

// using Authentication.Services;

// namespace Authentication.Middleware;


public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var host = context.Request.Host.Host; // e.g., schoolA.yourdomain.com
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

        // Fallback for local development execution or raw testing profiles
        if (string.IsNullOrEmpty(resolvedSubdomain) && context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerId))
        {
            resolvedSubdomain = headerId.ToString().ToLower();
        }

        // If no tenant context can be parsed out, halt bad multi-tenant requests safely
        if (string.IsNullOrEmpty(resolvedSubdomain))
        {
            // Allow bypassing for explicit global endpoints like a landing page or master registration
            if (context.Request.Path.StartsWithSegments("/api/global"))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Multi-tenant routing context missing. A valid subdomain is required." });
            return;
        }

        // 2. Cross-Verify Tenant Isolation with the JWT Token (If Present)
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var tokenString = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                
                if (handler.CanReadToken(tokenString))
                {
                    var jwtToken = handler.ReadJwtToken(tokenString);
                    var tokenTenantClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;

                    // If the token claims to belong to tenant B, but the user is browsing tenant A's subdomain, block it!
                    if (!string.IsNullOrEmpty(tokenTenantClaim) && !tokenTenantClaim.Equals(resolvedSubdomain, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { error = "Cross-tenant access violation. Access token mismatch with subdomain environment." });
                        return;
                    }
                }
            }
            catch
            {
                // Let normal Authentication middleware handle malformed tokens down the road
            }
        }

        // 3. Commit the verified tenant state to the Scoped service lifecycle
        tenantService.SetTenant(resolvedSubdomain);

        // Continue down the pipeline execution loop
        await _next(context);
    }
}
