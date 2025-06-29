namespace JwtProject.Models.Item;

public record CreateWorkItemDto(
    string Title,
    string Description,
    string Status,
    Guid? AssignToUserId);