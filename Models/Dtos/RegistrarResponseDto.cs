namespace Authentication.Models.Dtos;


public record RegistrarResponseDto(
    string Id, 
    string UserId, 
    string Email, 
    string FullName, 
    string EmployeeNumber, 
    string Department,
    string TenantId, 
    DateTime HireDate
);