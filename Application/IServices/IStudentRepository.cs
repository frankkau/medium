namespace Authentication.Application.IServices;

using Authentication.Models.Entity;
using Microsoft.EntityFrameworkCore;

public interface IStudentRepository
{
    Task<IEnumerable<StudentProfile>> GetAllAsync();
    Task<StudentProfile?> GetByIdAsync(string id);
    Task<StudentProfile?> GetByUserIdAsync(string userId);
    Task AddAsync(StudentProfile student);
    void Update(StudentProfile student);
    void Delete(StudentProfile student);
    Task<bool> SaveChangesAsync();
}
