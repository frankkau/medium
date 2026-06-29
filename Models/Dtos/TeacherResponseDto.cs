namespace Authentication.Models.Dtos;

public record TeacherResponseDto(
    string Id, 
    string UserId, 
    string Email, 
    string FullName, 
    string EmployeeNumber, 
    string Department, 
    DateTime HireDate
);
