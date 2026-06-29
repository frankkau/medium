

using Authentication.Models.Entity;

namespace Authentication.Application.IServices;

public interface IRegistrarRepository
{
    Task<IEnumerable<RegistrarProfile>> GetAllAsync();
    Task<RegistrarProfile?> GetByIdAsync(string id);
    Task AddAsync(RegistrarProfile registrar);
    void Update(RegistrarProfile registrar);
    void Delete(RegistrarProfile registrar);
    Task<bool> SaveChangesAsync();
}

