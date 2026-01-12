using IconProject.Common.Dtos;
using IconProject.Common.Dtos.Requests.Task;
using IconProject.Common.Dtos.Responses.Task;
using IconProject.Common.Enums;

namespace IconProject.Services.Interfaces;

public interface ITaskService
{
    Task<Result<IReadOnlyList<TaskResponse>>> GetAllByUserIdAsync(int userId, bool? isComplete = null, Priority? priority = null, CancellationToken cancellationToken = default);

    Task<Result<PaginatedTaskResponse>> GetPaginatedAsync(
        int userId,
        int page,
        int pageSize,
        bool? isComplete = null,
        Priority? priority = null,
        CancellationToken cancellationToken = default);

    Task<Result<TaskResponse>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Result<TaskResponse>> CreateAsync(int userId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<Result<TaskResponse>> UpdateAsync(int id, int userId, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Result<TaskResponse>> ToggleCompleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Result> UpdateSortOrderAsync(int userId, IReadOnlyList<(int TaskId, int SortOrder)> sortOrders, CancellationToken cancellationToken = default);
    Task<Result> ReorderTasksAsync(int userId, IReadOnlyList<int> taskIds, CancellationToken cancellationToken = default);
}
