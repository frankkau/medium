namespace Authentication.Application.IServices;

public interface ITenantService
{
    string? GetCurrentTenantId();
    void SetTenant(string tenantId);
}
