using IconProject.Common.Dtos;
using IconProject.Common.Dtos.Requests.Task;
using IconProject.Common.Dtos.Responses.Task;
using IconProject.Common.Enums;
using IconProject.Database.Models;
using IconProject.Database.UnitOfWork;
using IconProject.Services.Interfaces;

namespace IconProject.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TaskService> _logger;

    public TaskService(IUnitOfWork unitOfWork, ILogger<TaskService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<Result<IReadOnlyList<TaskResponse>>> GetAllByUserIdAsync(
        int userId,
        bool? isComplete = null,
        Priority? priority = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _unitOfWork.Tasks.FindAsync(t =>
            t.UserId == userId &&
            (!isComplete.HasValue || t.IsComplete == isComplete.Value) &&
            (!priority.HasValue || t.Priority == priority.Value));
        var response = tasks.Select(MapToResponse).ToList();

        _logger.LogInformation("Retrieved {Count} tasks for user {UserId} (isComplete: {IsComplete}, priority: {Priority})",
            response.Count, userId, isComplete, priority);

        return Result<IReadOnlyList<TaskResponse>>.Success(response);
    }
    
    public async Task<Result<PaginatedTaskResponse>> GetPaginatedAsync(
        int userId,
        int page,
        int pageSize,
        bool? isComplete = null,
        Priority? priority = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var skip = (page - 1) * pageSize;

        var (items, totalCount) = await _unitOfWork.Tasks.GetPaginatedAsync(
            skip,
            pageSize,
            t => t.UserId == userId &&
                 (!isComplete.HasValue || t.IsComplete == isComplete.Value) &&
                 (!priority.HasValue || t.Priority == priority.Value));

        var response = new PaginatedTaskResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        _logger.LogInformation(
            "Retrieved page {Page} of tasks for user {UserId}. Total: {TotalCount}",
            page, userId, totalCount);

        return Result<PaginatedTaskResponse>.Success(response);
    }
    
    public async Task<Result<TaskResponse>> GetByIdAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found", id);
            return DomainErrors.Task.NotFound(id);
        }

        if (task.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access task {TaskId} owned by user {OwnerId}",
                userId, id, task.UserId);
            return DomainErrors.Task.NotOwned;
        }

        return Result<TaskResponse>.Success(MapToResponse(task));
    }
    
    public async Task<Result<TaskResponse>> CreateAsync(
        int userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var userExists = await _unitOfWork.Users.ExistsAsync(u => u.Id == userId);
        if (!userExists)
        {
            _logger.LogWarning("Attempted to create task for non-existent user {UserId}", userId);
            return Error.NotFound("User", userId);
        }

        var task = new TaskEntity
        {
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Priority = request.Priority,
            SortOrder = request.SortOrder,
            IsComplete = false,
            UserId = userId
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created task {TaskId} for user {UserId}", task.Id, userId);

        return Result<TaskResponse>.Success(MapToResponse(task));
    }
    
    public async Task<Result<TaskResponse>> UpdateAsync(
        int id,
        int userId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for update", id);
            return DomainErrors.Task.NotFound(id);
        }

        if (task.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update task {TaskId} owned by user {OwnerId}",
                userId, id, task.UserId);
            return DomainErrors.Task.NotOwned;
        }
        
        task.Title = request.Title;
        task.Description = request.Description ?? string.Empty;
        task.IsComplete = request.IsComplete;
        task.Priority = request.Priority;
        task.SortOrder = request.SortOrder;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated task {TaskId}", id);

        return Result<TaskResponse>.Success(MapToResponse(task));
    }
    
    public async Task<Result> DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for deletion", id);
            return DomainErrors.Task.NotFound(id);
        }

        if (task.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete task {TaskId} owned by user {OwnerId}",
                userId, id, task.UserId);
            return DomainErrors.Task.NotOwned;
        }

        _unitOfWork.Tasks.Remove(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted task {TaskId}", id);

        return Result.Success();
    }
    
    public async Task<Result<TaskResponse>> ToggleCompleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for toggle complete", id);
            return DomainErrors.Task.NotFound(id);
        }

        if (task.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to toggle task {TaskId} owned by user {OwnerId}",
                userId, id, task.UserId);
            return DomainErrors.Task.NotOwned;
        }

        task.IsComplete = !task.IsComplete;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled task {TaskId} completion to {IsComplete}", id, task.IsComplete);

        return Result<TaskResponse>.Success(MapToResponse(task));
    }
    
    public async Task<Result> UpdateSortOrderAsync(
        int userId,
        IReadOnlyList<(int TaskId, int SortOrder)> sortOrders,
        CancellationToken cancellationToken = default)
    {
        if (sortOrders.Count == 0)
        {
            return Result.Success();
        }

        // Use transaction for batch update
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var (taskId, sortOrder) in sortOrders)
            {
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);

                if (task is null)
                {
                    _logger.LogWarning("Task {TaskId} not found for sort order update", taskId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return DomainErrors.Task.NotFound(taskId);
                }

                if (task.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update sort order for task {TaskId} owned by user {OwnerId}",
                        userId, taskId, task.UserId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return DomainErrors.Task.NotOwned;
                }

                task.SortOrder = sortOrder;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Updated sort order for {Count} tasks for user {UserId}",
                sortOrders.Count, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update sort order for user {UserId}", userId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<Result> ReorderTasksAsync(
        int userId,
        IReadOnlyList<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
        {
            return Result.Success();
        }

        var sortOrders = taskIds
            .Select((taskId, index) => (TaskId: taskId, SortOrder: index))
            .ToList();

        return await UpdateSortOrderAsync(userId, sortOrders, cancellationToken);
    }

    private static TaskResponse MapToResponse(TaskEntity entity) => new()
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
