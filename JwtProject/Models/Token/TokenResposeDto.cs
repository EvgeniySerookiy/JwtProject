namespace JwtProject.Models.Token;

public record TokenResposeDto(
    string AccessToken,
    string RefreshToken);