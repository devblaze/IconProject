using IconProject.Dtos;
using IconProject.Dtos.Task;

namespace IconProject.Services.Interfaces;

/// <summary>
/// Service interface for task operations.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Gets all tasks for a specific user.
    /// </summary>
    Task<Result<IReadOnlyList<TaskResponse>>> GetAllByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated tasks for a specific user with optional filtering.
    /// </summary>
    Task<Result<PaginatedTaskResponse>> GetPaginatedAsync(
        int userId,
        int page,
        int pageSize,
        bool? isComplete = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    Task<Result<TaskResponse>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new task.
    /// </summary>
    Task<Result<TaskResponse>> CreateAsync(int userId, CreateTaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    Task<Result<TaskResponse>> UpdateAsync(int id, int userId, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a task.
    /// </summary>
    Task<Result> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the completion status of a task.
    /// </summary>
    Task<Result<TaskResponse>> ToggleCompleteAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the sort order of multiple tasks (for drag-and-drop).
    /// </summary>
    Task<Result> UpdateSortOrderAsync(int userId, IReadOnlyList<(int TaskId, int SortOrder)> sortOrders, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders tasks based on the order of task IDs provided (for drag-and-drop).
    /// </summary>
    Task<Result> ReorderTasksAsync(int userId, IReadOnlyList<int> taskIds, CancellationToken cancellationToken = default);
}
