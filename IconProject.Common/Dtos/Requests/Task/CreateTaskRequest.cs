using System.ComponentModel.DataAnnotations;
using IconProject.Common.Enums;

namespace IconProject.Common.Dtos.Requests.Task;

public sealed record CreateTaskRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public int SortOrder { get; init; }
}
