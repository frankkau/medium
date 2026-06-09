using Authentication.Application.IServices;

namespace Authentication.Application.Service;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _currentTenantId;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        ResolveTenantFromSubdomain();
    }

    // Change the return type to string? (nullable string)
    public string? GetCurrentTenantId()
    {
        // DO NOT throw an exception here anymore! 
        // Just return the string, which will be null during startup seeding.
        return _currentTenantId;
    }

    public void SetTenant(string tenantId)
    {
        _currentTenantId = tenantId?.ToLower().Trim();
    }

    private void ResolveTenantFromSubdomain()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var host = context.Request.Host.Host; 
        var hostParts = host.Split('.');

        if (hostParts.Length > 2)
        {
            var subdomain = hostParts[0].ToLower();
            if (subdomain != "www" && subdomain != "api")
            {
                _currentTenantId = subdomain; 
                return;
            }
        }
        
        // Fallback for Postman local testing via Headers
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerId))
        {
            _currentTenantId = headerId.ToString().ToLower().Trim();
        }
    }
}