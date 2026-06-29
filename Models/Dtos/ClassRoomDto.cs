namespace Authentication.Models.Dtos;

// Models/Dtos/ClassroomDtos.cs
public record ClassroomCreateDto(
    int Grade,
    string Stream,
    int AcademicYear
);

public record AssignTeacherDto(
    string TeacherId,
    bool IsPrimaryClassTeacher
);

public record ClassroomResponseDto(
    string Id,
    string Name,
    int Grade,
    string Stream,
    int AcademicYear,
    string TenantId,
    List<ClassroomTeacherDto> Teachers,
    int StudentCount
);

public record ClassroomTeacherDto(
    string TeacherId,
    string TeacherName,
    string EmployeeNumber,
    bool IsPrimaryClassTeacher,
    DateTime AssignedAt
);

public record ClassroomStudentDto(
    string StudentId,
    string FullName,
    string StudentNumber,
    string Email
);

// Attendance
public record AttendanceSubmitDto(
    string ClassroomId,
    DateOnly Date,
    List<StudentAttendanceDto> Records
);

public record StudentAttendanceDto(
    string StudentId,
    AttendanceStatus Status,
    string? Remarks
);

public enum AttendanceStatus
{
    Present,
    Absent,
    Late,
    Excused,
    Sick
}

public record AttendanceResponseDto(
    string Id,
    string StudentId,
    string StudentName,
    DateOnly Date,
    AttendanceStatus Status,
    string? Remarks
);
