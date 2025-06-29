namespace JwtProject.Models.User;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string Role);