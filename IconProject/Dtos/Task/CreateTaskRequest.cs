using System.ComponentModel.DataAnnotations;
using IconProject.Database.Models;

namespace IconProject.Dtos.Task;

/// <summary>
/// Request DTO for creating a new task.
/// </summary>
public sealed record CreateTaskRequest
{
    /// <summary>
    /// The title of the task.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    /// <summary>
    /// Optional description of the task.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; init; }

    /// <summary>
    /// The priority of the task (Low=0, Medium=1, High=2).
    /// </summary>
    public Priority Priority { get; init; } = Priority.Medium;

    /// <summary>
    /// Optional sort order for drag-and-drop functionality.
    /// </summary>
    public int SortOrder { get; init; }
}
