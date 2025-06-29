namespace JwtProject.Models;

public record TokenResposeDto(
    string AccessToken,
    string RefreshToken);