using System.ComponentModel.DataAnnotations;
using IconProject.Common.Enums;

namespace IconProject.Common.Dtos.Requests.Task;

public sealed record UpdateTaskRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    public bool IsComplete { get; init; }

    public Priority Priority { get; init; }

    public int SortOrder { get; init; }
}
