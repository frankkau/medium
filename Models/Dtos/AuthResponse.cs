// DTOs/AuthDtos.cs
public record AuthResponse(
    string Message,
    string? AccessToken = null,
    string? RefreshToken = null,
    bool Success = true
);