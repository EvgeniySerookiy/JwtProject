namespace JwtProject.Models;

public record RefreshTokenRequestDto(
    Guid UserId,
    string RefreshToken);