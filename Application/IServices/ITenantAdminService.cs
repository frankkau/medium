using Authentication.Models.Dtos;

namespace Authentication.Application.IServices;




public interface ITenantAdminService
{
    Task<TenantResponse> CreateTenantAsync(CreateTenantRequest request);
    Task<TenantResponse?> GetTenantByIdAsync(string id);
    Task<IEnumerable<TenantResponse>> GetAllTenantsAsync();
    Task<TenantResponse> UpdateTenantAsync(string id, UpdateTenantDto request);
    Task<bool> DeleteTenantAsync(string id);
}

