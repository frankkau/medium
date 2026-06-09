using Authentication.Application.IServices;
using Authentication.Models.Dtos;
using Authentication.Models.Entity;

namespace Authentication.Application.Service;

public class TenantAdminService : ITenantAdminService
{
    private readonly ITenantRepository _repository;
    private readonly ILogger<TenantAdminService> _logger;

    public TenantAdminService(ITenantRepository repository, ILogger<TenantAdminService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TenantResponse> CreateTenantAsync(CreateTenantRequest request)
    {
        try
        {
            var cleanedSubdomain = request.Subdomain.ToLower().Trim();
            
            // Business Validation Rule: Subdomains must be unique across the SaaS platform
            var existingTenant = await _repository.GetBySubdomainAsync(cleanedSubdomain);
            if (existingTenant != null)
            {
                throw new InvalidOperationException($"Subdomain '{cleanedSubdomain}' is already assigned to an active tenant.");
            }

            var tenant = new Tenant
            {
                Id = request.Id.Trim(),
                Name = request.Name.Trim(),
                Subdomain = cleanedSubdomain,
                IsActive = true
            };

            await _repository.AddAsync(tenant);
            _logger.LogInformation("Successfully initialized new tenant structure: {TenantId} on subdomain {Subdomain}", tenant.Id, tenant.Subdomain);

            return new TenantResponse(tenant.Id, tenant.Name, tenant.Subdomain, tenant.IsActive);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "An infrastructure execution fault occurred while generating tenant profile for ID: {TenantId}", request.Id);
            throw new Exception("A fatal service error occurred while writing tenant details to persistent storage.", ex);
        }
    }

    public async Task<TenantResponse?> GetTenantByIdAsync(string id)
    {
        try
        {
            var tenant = await _repository.GetByIdAsync(id);
            if (tenant == null) return null;

            return new TenantResponse(tenant.Id, tenant.Name, tenant.Subdomain, tenant.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred fetching details for tenant ID {TenantId}", id);
            throw new Exception("Unable to pull requested customer instance record at this time.");
        }
    }

    public async Task<IEnumerable<TenantResponse>> GetAllTenantsAsync()
    {
        try
        {
            var tenants = await _repository.GetAllAsync();
            return tenants.Select(t => new TenantResponse(t.Id, t.Name, t.Subdomain, t.IsActive));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure reading master tenant registry list.");
            throw new Exception("Failed to query multi-tenant infrastructure directories.");
        }
    }

    public async Task<TenantResponse> UpdateTenantAsync(string id, UpdateTenantRequest request)
    {
        try
        {
            var tenant = await _repository.GetByIdAsync(id) 
                ?? throw new KeyNotFoundException($"Tenant profile reference ID '{id}' could not be resolved.");

            tenant.Name = request.Name.Trim();
            tenant.IsActive = request.IsActive;

            await _repository.UpdateAsync(tenant);
            _logger.LogInformation("Tenant modification sequence verified for Target ID: {TenantId}", id);

            return new TenantResponse(tenant.Id, tenant.Name, tenant.Subdomain, tenant.IsActive);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Unexpected mutation runtime fault updating tenant ID {TenantId}", id);
            throw new Exception("Persistence modifications rejected during engine commit phase.");
        }
    }

    public async Task<bool> DeleteTenantAsync(string id)
    {
        try
        {
            var tenant = await _repository.GetByIdAsync(id);
            if (tenant == null) return false;

            await _repository.DeleteAsync(tenant);
            _logger.LogWarning("Tenant cluster mapping records completely removed. Identifier Hash: {TenantId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catastrophic failure attempting erasure sequence for Tenant ID {TenantId}", id);
            throw new Exception("Safety isolation protocols prevented database entity teardown sequences.");
        }
    }
}

