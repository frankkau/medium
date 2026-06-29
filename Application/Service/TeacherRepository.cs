using Authentication.Application.IServices;
using Authentication.Data;
using Authentication.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

public class TeacherRepository : ITeacherRepository
{
    private readonly ApplicationDbContext _context;

    public TeacherRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TeacherProfile>> GetAllAsync()
    {
        return await _context.TeacherProfiles
            .Include(t => t.User)
            .ToListAsync();
    }

    public async Task<TeacherProfile?> GetByIdAsync(string id)
    {
        return await _context.TeacherProfiles
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(TeacherProfile teacher)
    {
        await _context.TeacherProfiles.AddAsync(teacher);
    }

    public void Update(TeacherProfile teacher)
    {
        _context.Entry(teacher).State = EntityState.Modified;
    }

    public void Delete(TeacherProfile teacher)
    {
        _context.TeacherProfiles.Remove(teacher);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}