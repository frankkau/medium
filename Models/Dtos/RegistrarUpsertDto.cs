namespace Authentication.Models.Dtos;


public record RegistrarUpsertDto(
    string Email, 
    string FullName, 
    string EmployeeNumber, 
    string Department, 
    DateTime HireDate
);
