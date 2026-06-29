

using Authentication.Models.Entity;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(string id);
    Task<Tenant?> GetByIdOrSubdomainAsync(string identifier);
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task AddAsync(Tenant tenant);
    Task UpdateAsync(Tenant tenant);
    Task DeleteAsync(Tenant tenant);
}

