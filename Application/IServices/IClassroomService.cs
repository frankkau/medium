using Authentication.Models.Dtos;

namespace Authentication.Application.IServices;

// Application/IServices/IClassroomService.cs
public interface IClassroomService
{
    Task<ClassroomResponseDto> CreateClassroomAsync(ClassroomCreateDto dto);
    Task<IEnumerable<ClassroomResponseDto>> GetAllClassroomsAsync();
    Task<ClassroomResponseDto> GetClassroomByIdAsync(string id);
    Task AssignTeacherAsync(string classroomId, AssignTeacherDto dto);
    Task RemoveTeacherAsync(string classroomId, string teacherId);
    Task<IEnumerable<ClassroomStudentDto>> GetStudentsInClassroomAsync(string classroomId);
    Task SubmitAttendanceAsync(string teacherId, AttendanceSubmitDto dto);
    Task<IEnumerable<AttendanceResponseDto>> GetAttendanceAsync(string classroomId, DateOnly date);
    Task<IEnumerable<ClassroomResponseDto>> GetTeacherClassroomsAsync(string teacherId);
}
