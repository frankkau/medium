using Authentication.Application.IServices;
using Authentication.Data;
using Authentication.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;


public class RegistrarRepository : IRegistrarRepository
{
    private readonly ApplicationDbContext _context;

    public RegistrarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RegistrarProfile>> GetAllAsync()
    {
        return await _context.RegistrarProfiles
            .Include(t => t.User)
            .ToListAsync();
    }

    public async Task<RegistrarProfile?> GetByIdAsync(string id)
    {
        return await _context.RegistrarProfiles
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(RegistrarProfile registrar)
    {
        await _context.RegistrarProfiles.AddAsync(registrar);
    }

    public void Update(RegistrarProfile registrar)
    {
        _context.RegistrarProfiles.Update(registrar);
    }

    public void Delete(RegistrarProfile registrar)
    {
        _context.RegistrarProfiles.Remove(registrar);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}