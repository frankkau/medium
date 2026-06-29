using Authentication.Application.IServices;

namespace Authentication.Application.Service;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _overrideTenantId; // Only set by SetTenant()

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        // ✅ Don't resolve here — HttpContext may not exist yet
    }

    public string? GetCurrentTenantId()
    {
        // Manual override takes priority (used during seeding)
        if (!string.IsNullOrEmpty(_overrideTenantId))
            return _overrideTenantId;

        // Lazily resolve from the current HTTP request
        return ResolveTenantFromSubdomain();
    }

    public void SetTenant(string tenantId)
    {
        _overrideTenantId = tenantId?.ToLower().Trim();
    }

    private string? ResolveTenantFromSubdomain()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        var host = context.Request.Host.Host;
        var hostParts = host.Split('.');

        if (hostParts.Length > 2)
        {
            var subdomain = hostParts[0].ToLower();
            if (subdomain != "www" && subdomain != "api")
                return subdomain;
        }

        // Fallback for Postman/testing via header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerId))
            return headerId.ToString().ToLower().Trim();

        return null;
    }
}