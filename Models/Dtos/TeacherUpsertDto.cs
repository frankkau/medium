namespace Authentication.Models.Dtos;

using System;

public record TeacherUpsertDto(
    string Email, 
    string FullName, 
    string EmployeeNumber, 
    string Department, 
    DateTime HireDate
);
