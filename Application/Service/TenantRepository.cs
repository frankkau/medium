using Authentication.Data;
using Authentication.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRepository(ApplicationDbContext context) => _context = context;

    public async Task<Tenant?> GetByIdAsync(string id) => 
        await _context.Tenants.FindAsync(id);

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain) => 
        await _context.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLower().Trim());

    public async Task<IEnumerable<Tenant>> GetAllAsync() => 
        await _context.Tenants.AsNoTracking().ToListAsync();

    public async Task AddAsync(Tenant tenant) => 
        await _context.Tenants.AddAsync(tenant);

    public async Task UpdateAsync(Tenant tenant)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Tenant tenant)
    {
        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();
    }
}

