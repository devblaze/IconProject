namespace IconProject.Database.Models;

public class TaskEntity : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public int SortOrder { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
