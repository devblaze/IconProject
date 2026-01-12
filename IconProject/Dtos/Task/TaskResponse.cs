using IconProject.Database.Models;

namespace IconProject.Dtos.Task;

/// <summary>
/// Response DTO representing a task.
/// </summary>
public sealed record TaskResponse
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public bool IsComplete { get; init; }
    public Priority Priority { get; init; }
    public int SortOrder { get; init; }
    public int UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Maps a TaskEntity to a TaskResponse.
    /// </summary>
    public static TaskResponse FromEntity(TaskEntity entity)
    {
        return new TaskResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            IsComplete = entity.IsComplete,
            Priority = entity.Priority,
            SortOrder = entity.SortOrder,
            UserId = entity.UserId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}

/// <summary>
/// Response DTO for paginated task results.
/// </summary>
public sealed record PaginatedTaskResponse
{
    public required IReadOnlyList<TaskResponse> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
