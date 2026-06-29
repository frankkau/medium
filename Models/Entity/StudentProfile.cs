using Authentication.Application.IServices;

namespace Authentication.Models.Entity;

public class StudentProfile : IMustHaveTenant
{
    public string Id { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string EnrollmentStatus { get; set; } = null!;
    public string TenantId { get; set; } = null!;

    // Foreign Key matching IdentityUser
    public string UserId { get; set; } = null!;
    public virtual User User { get; set; } = null!;

    // Add to StudentProfile.cs
    public string? ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
}
