namespace JwtProject.Models.Item;

public record UpdateWorkItemDto(
    string? Title,
    string? Description,
    string? Status,
    Guid? AssignToUserId);