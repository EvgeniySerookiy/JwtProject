namespace JwtProject.Entities;

public class WorkItem
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
}
