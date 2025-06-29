namespace JwtProject.Models;

public record UserRegisterDto(
    string Username,
    string Password,
    string Role);