namespace JwtProject.Models.Item;

public record WorkItemDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTime CreatedAt,
    string CreatedByUsername);