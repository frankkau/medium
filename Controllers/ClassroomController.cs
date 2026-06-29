using Authentication.Application.IServices;
using Authentication.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Controllers;

// Controllers/ClassroomController.cs
[ApiController]
[Route("api/classrooms")]
[Authorize]
public class ClassroomController : ControllerBase
{
    private readonly IClassroomService _classroomService;

    public ClassroomController(IClassroomService classroomService)
    {
        _classroomService = classroomService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Registrar")]
    public async Task<IActionResult> Create([FromBody] ClassroomCreateDto dto)
    {
        var result = await _classroomService.CreateClassroomAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Registrar,Teacher")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _classroomService.GetAllClassroomsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Registrar,Teacher")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _classroomService.GetClassroomByIdAsync(id);
        return Ok(result);
    }

    [HttpPost("{classroomId}/teachers")]
    [Authorize(Roles = "Admin,Registrar")]
    public async Task<IActionResult> AssignTeacher(string classroomId, [FromBody] AssignTeacherDto dto)
    {
        await _classroomService.AssignTeacherAsync(classroomId, dto);
        return Ok(new { message = "Teacher assigned successfully." });
    }

    [HttpDelete("{classroomId}/teachers/{teacherId}")]
    [Authorize(Roles = "Admin,Registrar")]
    public async Task<IActionResult> RemoveTeacher(string classroomId, string teacherId)
    {
        await _classroomService.RemoveTeacherAsync(classroomId, teacherId);
        return Ok(new { message = "Teacher removed successfully." });
    }

    [HttpGet("{classroomId}/students")]
    [Authorize(Roles = "Admin,Registrar,Teacher")]
    public async Task<IActionResult> GetStudents(string classroomId)
    {
        var result = await _classroomService.GetStudentsInClassroomAsync(classroomId);
        return Ok(result);
    }

    [HttpPost("attendance")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> SubmitAttendance([FromBody] AttendanceSubmitDto dto)
    {
        // Get teacherId from the JWT claims
        var teacherId = User.FindFirst("teacher_profile_id")?.Value
            ?? throw new UnauthorizedAccessException("Teacher profile not found in token.");

        await _classroomService.SubmitAttendanceAsync(teacherId, dto);
        return Ok(new { message = "Attendance submitted successfully." });
    }

    [HttpGet("{classroomId}/attendance")]
    [Authorize(Roles = "Admin,Registrar,Teacher")]
    public async Task<IActionResult> GetAttendance(string classroomId, [FromQuery] DateOnly date)
    {
        var result = await _classroomService.GetAttendanceAsync(classroomId, date);
        return Ok(result);
    }

    [HttpGet("my-classrooms/{teacherId}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetTeacherClassrooms(string teacherId)
    {
        var result = await _classroomService.GetTeacherClassroomsAsync(teacherId);
        return Ok(result);
    }
}
