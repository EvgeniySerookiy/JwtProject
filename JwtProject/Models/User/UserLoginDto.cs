namespace JwtProject.Models.User;

public record UserLoginDto(
    string Username,
    string Password);