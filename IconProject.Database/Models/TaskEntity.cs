namespace IconProject.Database.Models;

public class TaskEntity : BaseEntity
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsComplete { get; set; }
    public Status Status { get; set; }
    public int SortOrder { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
}