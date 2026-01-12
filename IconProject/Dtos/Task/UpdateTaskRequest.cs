using System.ComponentModel.DataAnnotations;
using IconProject.Database.Models;

namespace IconProject.Dtos.Task;

/// <summary>
/// Request DTO for updating an existing task.
/// </summary>
public sealed record UpdateTaskRequest
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
    /// Whether the task is completed.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// The priority of the task (Low=0, Medium=1, High=2).
    /// </summary>
    public Priority Priority { get; init; }

    /// <summary>
    /// Optional sort order for drag-and-drop functionality.
    /// </summary>
    public int SortOrder { get; init; }
}
