using Authentication.Application.IServices;

namespace Authentication.Models.Entity;

public class Classroom : IMustHaveTenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = null!;        // e.g. "8A", "9B"
    public int Grade { get; set; }                    // e.g. 8, 9, 10
    public string Stream { get; set; } = null!;       // e.g. "A", "B"
    public int AcademicYear { get; set; }             // e.g. 2025
    public string TenantId { get; set; } = null!;

    public ICollection<ClassroomTeacher> ClassroomTeachers { get; set; } = new List<ClassroomTeacher>();
    public ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();
}
