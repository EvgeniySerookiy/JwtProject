namespace JwtProject.Models.User;

public record UserRegisterDto(
    string Username,
    string Password,
    string Role,
    string Email);