namespace JwtProject.Models.Token;

public record RefreshTokenRequestDto(
    Guid UserId,
    string RefreshToken);