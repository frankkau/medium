using Authentication.Application.IServices;
using Authentication.Data;
using Authentication.Models.Dtos;
using Authentication.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

// Application/Service/ClassroomService.cs
public class ClassroomService : IClassroomService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public ClassroomService(ApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    private string RequireTenantId() =>
        _tenantService.GetCurrentTenantId()
        ?? throw new InvalidOperationException("No active tenant context.");

    public async Task<ClassroomResponseDto> CreateClassroomAsync(ClassroomCreateDto dto)
    {
        var tenantId = RequireTenantId();

        var exists = await _context.Classrooms.AnyAsync(c =>
            c.Grade == dto.Grade &&
            c.Stream == dto.Stream &&
            c.AcademicYear == dto.AcademicYear);

        if (exists)
            throw new InvalidOperationException($"Classroom Grade {dto.Grade}{dto.Stream} already exists for {dto.AcademicYear}.");

        var classroom = new Classroom
        {
            Id           = Guid.NewGuid().ToString(),
            Grade        = dto.Grade,
            Stream       = dto.Stream.ToUpper().Trim(),
            Name         = $"Grade {dto.Grade}{dto.Stream.ToUpper()}",
            AcademicYear = dto.AcademicYear,
            TenantId     = tenantId
        };

        _context.Classrooms.Add(classroom);
        await _context.SaveChangesAsync();

        return MapToDto(classroom);
    }

    public async Task AssignTeacherAsync(string classroomId, AssignTeacherDto dto)
    {
        var tenantId = RequireTenantId();

        var classroom = await _context.Classrooms
            .Include(c => c.ClassroomTeachers)
            .FirstOrDefaultAsync(c => c.Id == classroomId)
            ?? throw new KeyNotFoundException($"Classroom '{classroomId}' not found.");

        var teacher = await _context.TeacherProfiles
            .FirstOrDefaultAsync(t => t.Id == dto.TeacherId)
            ?? throw new KeyNotFoundException($"Teacher '{dto.TeacherId}' not found.");

        var alreadyAssigned = classroom.ClassroomTeachers
            .Any(ct => ct.TeacherId == dto.TeacherId);

        if (alreadyAssigned)
            throw new InvalidOperationException("Teacher is already assigned to this classroom.");

        // If setting as primary, demote existing primary
        if (dto.IsPrimaryClassTeacher)
        {
            var existingPrimary = classroom.ClassroomTeachers
                .FirstOrDefault(ct => ct.IsPrimaryClassTeacher);

            if (existingPrimary != null)
                existingPrimary.IsPrimaryClassTeacher = false;
        }

        _context.ClassroomTeachers.Add(new ClassroomTeacher
        {
            Id                   = Guid.NewGuid().ToString(),
            ClassroomId          = classroomId,
            TeacherId            = dto.TeacherId,
            IsPrimaryClassTeacher = dto.IsPrimaryClassTeacher,
            AssignedAt           = DateTime.UtcNow,
            TenantId             = tenantId
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveTeacherAsync(string classroomId, string teacherId)
    {
        var assignment = await _context.ClassroomTeachers
            .FirstOrDefaultAsync(ct => ct.ClassroomId == classroomId && ct.TeacherId == teacherId)
            ?? throw new KeyNotFoundException("Teacher assignment not found.");

        _context.ClassroomTeachers.Remove(assignment);
        await _context.SaveChangesAsync();
    }

    public async Task SubmitAttendanceAsync(string teacherId, AttendanceSubmitDto dto)
    {
        var tenantId = RequireTenantId();

        // Verify teacher belongs to this classroom
        var isAssigned = await _context.ClassroomTeachers
            .AnyAsync(ct => ct.ClassroomId == dto.ClassroomId && ct.TeacherId == teacherId);

        if (!isAssigned)
            throw new UnauthorizedAccessException("You are not assigned to this classroom.");

        // Remove existing attendance for that date if resubmitting
        var existing = await _context.Attendances
            .Where(a => a.ClassroomId == dto.ClassroomId && a.Date == dto.Date)
            .ToListAsync();

        _context.Attendances.RemoveRange(existing);

        var records = dto.Records.Select(r => new Attendance
        {
            Id                   = Guid.NewGuid().ToString(),
            ClassroomId          = dto.ClassroomId,
            StudentId            = r.StudentId,
            RecordedByTeacherId  = teacherId,
            Date                 = dto.Date,
            Status               = r.Status,
            Remarks              = r.Remarks,
            TenantId             = tenantId
        }).ToList();

        _context.Attendances.AddRange(records);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceAsync(string classroomId, DateOnly date)
    {
        return await _context.Attendances
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Where(a => a.ClassroomId == classroomId && a.Date == date)
            .Select(a => new AttendanceResponseDto(
                a.Id,
                a.StudentId,
                a.Student.User.FullName,
                a.Date,
                a.Status,
                a.Remarks))
            .ToListAsync();
    }

    public async Task<IEnumerable<ClassroomStudentDto>> GetStudentsInClassroomAsync(string classroomId)
    {
        return await _context.StudentProfiles
            .Include(s => s.User)
            .Where(s => s.ClassroomId == classroomId)
            .Select(s => new ClassroomStudentDto(
                s.Id,
                s.User.FullName,
                s.StudentNumber,
                s.User.Email!))
            .ToListAsync();
    }

    public async Task<IEnumerable<ClassroomResponseDto>> GetTeacherClassroomsAsync(string teacherId)
    {
        var classrooms = await _context.Classrooms
            .Include(c => c.ClassroomTeachers).ThenInclude(ct => ct.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Students)
            .Where(c => c.ClassroomTeachers.Any(ct => ct.TeacherId == teacherId))
            .ToListAsync();

        return classrooms.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassroomResponseDto>> GetAllClassroomsAsync()
    {
        var classrooms = await _context.Classrooms
            .Include(c => c.ClassroomTeachers).ThenInclude(ct => ct.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Students)
            .ToListAsync();

        return classrooms.Select(MapToDto);
    }

    public async Task<ClassroomResponseDto> GetClassroomByIdAsync(string id)
    {
        var classroom = await _context.Classrooms
            .Include(c => c.ClassroomTeachers).ThenInclude(ct => ct.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Classroom '{id}' not found.");

        return MapToDto(classroom);
    }

    private static ClassroomResponseDto MapToDto(Classroom c) => new(
        c.Id,
        c.Name,
        c.Grade,
        c.Stream,
        c.AcademicYear,
        c.TenantId,
        c.ClassroomTeachers.Select(ct => new ClassroomTeacherDto(
            ct.TeacherId,
            ct.Teacher?.User?.FullName ?? "",
            ct.Teacher?.EmployeeNumber ?? "",
            ct.IsPrimaryClassTeacher,
            ct.AssignedAt)).ToList(),
        c.Students.Count
    );
}
