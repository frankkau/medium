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
                Motto =request.Motto,
                MissionStatement = request.MissionStatement,
                VisionStatement = request.MissionStatement,
                LogoUrl = request.LogoUrl,
                FaviconUrl = request.FaviconUrl,
                PrimaryColor = request.PrimaryColor,                
                SecondaryColor = request.SecondaryColor,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                PhysicalAddress = request.PhysicalAddress,
                // CreatedAt = request.DateTime.UtcNow,
                IsActive = true
            };

            _logger.LogInformation("Before saving tenant...");
            await _repository.AddAsync(tenant);
            _logger.LogInformation("After saving tenan,t...");
            return new TenantResponse(
                tenant.Id,
                tenant.Name,
                tenant.Subdomain,
                tenant.Motto,
                tenant.MissionStatement,
                tenant.VisionStatement,
                tenant.LogoUrl,
                tenant.FaviconUrl,
                tenant.PrimaryColor,
                tenant.SecondaryColor,
                tenant.ContactEmail,
                tenant.ContactPhone,
                tenant.PhysicalAddress,
                tenant.IsActive,
                tenant.CreatedAt
            );
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
            var tenant = await _repository.GetBySubdomainAsync(id);
            if (tenant == null) return null;

            return new TenantResponse(
                tenant.Id,
                tenant.Name,
                tenant.Subdomain,
                tenant.Motto,
                tenant.MissionStatement,
                tenant.VisionStatement,
                tenant.LogoUrl,
                tenant.FaviconUrl,
                tenant.PrimaryColor,
                tenant.SecondaryColor,
                tenant.ContactEmail,
                tenant.ContactPhone,
                tenant.PhysicalAddress,
                tenant.IsActive,
                tenant.CreatedAt
            );
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
            return tenants.Select(t => new TenantResponse(
                t.Id,
                t.Name,
                t.Subdomain,
                t.Motto,
                t.MissionStatement,
                t.VisionStatement,
                t.LogoUrl,
                t.FaviconUrl,
                t.PrimaryColor,
                t.SecondaryColor,
                t.ContactEmail,
                t.ContactPhone,
                t.PhysicalAddress,
                t.IsActive,
                t.CreatedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure reading master tenant registry list.");
            throw new Exception("Failed to query multi-tenant infrastructure directories.");
        }
    }

    public async Task<TenantResponse> UpdateTenantAsync(string id, UpdateTenantDto request)
    {
        try
        {
        // Change this line inside your TenantAdminService.cs
         var tenant = await _repository.GetByIdOrSubdomainAsync(id)
                 ?? throw new KeyNotFoundException($"Tenant profile with subdomain '{id}' could not be resolved.");
        // --- Core Branding & Identity ---
        tenant.Name = request.Name?.Trim() ?? tenant.Name;
        tenant.Motto = request.Motto?.Trim() ?? tenant.Motto;
        tenant.MissionStatement = request.MissionStatement?.Trim() ?? tenant.MissionStatement;
        tenant.VisionStatement = request.VisionStatement?.Trim() ?? tenant.VisionStatement;
        tenant.LogoUrl = request.LogoUrl?.Trim() ?? tenant.LogoUrl;
        tenant.FaviconUrl = request.FaviconUrl?.Trim() ?? tenant.FaviconUrl;

        // --- Dynamic UI Customization (Theme Matching) ---
        tenant.PrimaryColor = request.PrimaryColor?.Trim() ?? tenant.PrimaryColor;
        tenant.SecondaryColor = request.SecondaryColor?.Trim() ?? tenant.SecondaryColor;

        // --- Official Infrastructure & Contact Metadata ---
        tenant.ContactEmail = request.ContactEmail?.Trim() ?? tenant.ContactEmail;
        tenant.ContactPhone = request.ContactPhone?.Trim() ?? tenant.ContactPhone;
        tenant.PhysicalAddress = request.PhysicalAddress?.Trim() ?? tenant.PhysicalAddress;

        // --- Academic & System State Controls ---
        // tenant.CurrentAcademicYear = request.CurrentAcademicYear?.Trim() ?? tenant.CurrentAcademicYear;
        // tenant.CurrentTermOrSemester = request.CurrentTermOrSemester?.Trim() ?? tenant.CurrentTermOrSemester;
        tenant.IsActive = request.IsActive ?? tenant.IsActive;

        // Commit mutations to the database provider
        await _repository.UpdateAsync(tenant);
        _logger.LogInformation("Tenant modification sequence verified for Target ID: {TenantId}", id);

        // Return the fully populated structural DTO response
        return new TenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.Motto,
            tenant.MissionStatement,
            tenant.VisionStatement,
            tenant.LogoUrl,
            tenant.FaviconUrl,
            tenant.PrimaryColor,
            tenant.SecondaryColor,
            tenant.ContactEmail,
            tenant.ContactPhone,
            tenant.PhysicalAddress,           
            tenant.IsActive,
            tenant.CreatedAt
        );
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
            var tenant = await _repository.GetByIdOrSubdomainAsync(id);
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

