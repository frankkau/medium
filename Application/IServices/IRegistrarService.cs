using Authentication.Models.Dtos;

namespace Authentication.Application.IServices;

public interface IRegistrarService
{
     Task<IEnumerable<RegistrarResponseDto>> GetAllRegistrarsAsync();
    Task<RegistrarResponseDto> GetRegistrarByIdAsync(string id);
    Task<RegistrarResponseDto> RegisterRegistrarAsync(RegistrarUpsertDto dto);
    Task UpdateRegistrarAsync(string id, RegistrarUpsertDto dto);
    Task DeleteRegistrarAsync(string id);
}
