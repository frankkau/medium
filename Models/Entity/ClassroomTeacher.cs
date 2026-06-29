using Authentication.Application.IServices;

namespace Authentication.Models.Entity;

// Models/Entity/ClassroomTeacher.cs — join table for many-to-many
public class ClassroomTeacher : IMustHaveTenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClassroomId { get; set; } = null!;
    public string TeacherId { get; set; } = null!;   // FK to TeacherProfile
    public bool IsPrimaryClassTeacher { get; set; }  // one is primary, others are co-teachers
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string TenantId { get; set; } = null!;

    public Classroom Classroom { get; set; } = null!;
    public TeacherProfile Teacher { get; set; } = null!;
}
