using Authentication.Application.IServices;
using Authentication.Models.Dtos;

namespace Authentication.Models.Entity;

// Models/Entity/Attendance.cs
public class Attendance : IMustHaveTenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClassroomId { get; set; } = null!;
    public string StudentId { get; set; } = null!;
    public string RecordedByTeacherId { get; set; } = null!;
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Remarks { get; set; }
    public string TenantId { get; set; } = null!;

    public Classroom Classroom { get; set; } = null!;
    public StudentProfile Student { get; set; } = null!;
    public TeacherProfile RecordedBy { get; set; } = null!;
}
