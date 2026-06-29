namespace Authentication.Models.Dtos;

public record StudentResponseDto(string Id, string UserId, string Email, string FullName, string StudentNumber, DateTime DateOfBirth, string EnrollmentStatus);
