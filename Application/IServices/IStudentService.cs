using Authentication.Models.Dtos;

namespace Authentication.Application.IServices;

public interface IStudentService
{
    Task<IEnumerable<StudentResponseDto>> GetAllStudentsAsync();
    Task<StudentResponseDto> GetStudentByIdAsync(string id);
    Task<StudentResponseDto> CreateStudentAsync(StudentUpsertDto dto);
    Task UpdateStudentAsync(string id, StudentUpsertDto dto);
    Task DeleteStudentAsync(string id);
}
