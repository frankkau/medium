using Authentication.Models.Entity;

namespace Authentication.Application.IServices;

public interface ITeacherRepository
{
    Task<IEnumerable<TeacherProfile>> GetAllAsync();
    Task<TeacherProfile?> GetByIdAsync(string id);
    Task AddAsync(TeacherProfile teacher);
    void Update(TeacherProfile teacher);
    void Delete(TeacherProfile teacher);
    Task<bool> SaveChangesAsync();
}
