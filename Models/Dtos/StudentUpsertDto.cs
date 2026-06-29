namespace Authentication.Models.Dtos;

public record StudentUpsertDto(string Email, string FullName, string StudentNumber, DateTime DateOfBirth, string EnrollmentStatus);
