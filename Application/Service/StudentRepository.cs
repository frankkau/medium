using Authentication.Application.IServices;
using Authentication.Data;
using Authentication.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

public class StudentRepository : IStudentRepository
{
    private readonly ApplicationDbContext _context;

    public StudentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StudentProfile>> GetAllAsync()
    {
        return await _context.StudentProfiles.Include(s => s.User).ToListAsync();
    }

    public async Task<StudentProfile?> GetByIdAsync(string id)
    {
        return await _context.StudentProfiles.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<StudentProfile?> GetByUserIdAsync(string userId)
    {
        return await _context.StudentProfiles.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task AddAsync(StudentProfile student)
    {
        await _context.StudentProfiles.AddAsync(student);
    }

    public void Update(StudentProfile student)
    {
        _context.Entry(student).State = EntityState.Modified;
    }

    public void Delete(StudentProfile student)
    {
        _context.StudentProfiles.Remove(student);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}

// ==========================================
//