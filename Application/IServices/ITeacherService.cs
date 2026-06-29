using Authentication.Models.Dtos;

namespace Authentication.Application.IServices;

public interface ITeacherService
{
    Task<IEnumerable<TeacherResponseDto>> GetAllTeachersAsync();
    Task<TeacherResponseDto> GetTeacherByIdAsync(string id);
    Task<TeacherResponseDto> RegisterTeacherAsync(TeacherUpsertDto dto);
    Task UpdateTeacherAsync(string id, TeacherUpsertDto dto);
    Task DeleteTeacherAsync(string id);
}

