using System.Security.Claims;
using IconProject.Dtos;
using IconProject.Dtos.Task;
using IconProject.Extensions;
using IconProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IconProject.Controllers;

/// <summary>
/// API controller for task management operations.
/// Requires JWT authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Gets all tasks for the authenticated user.
    /// </summary>
    /// <returns>A list of tasks.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.GetAllByUserIdAsync(userId.Value, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Gets paginated tasks for the authenticated user.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 100).</param>
    /// <param name="isComplete">Filter by completion status.</param>
    /// <returns>Paginated task results.</returns>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PaginatedTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedTaskResponse>> GetPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isComplete = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.GetPaginatedAsync(userId.Value, page, pageSize, isComplete, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Gets a specific task by ID.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <returns>The task details.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.GetByIdAsync(id, userId.Value, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="request">The task creation request.</param>
    /// <returns>The created task.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.CreateAsync(userId.Value, request, cancellationToken);

        return result.Match(
            onSuccess: task => CreatedAtAction(
                nameof(GetById),
                new { id = task.Id },
                task),
            onFailure: _ => result.ToActionResult(Request.Path));
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated task.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(
        int id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.UpdateAsync(id, userId.Value, request, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.DeleteAsync(id, userId.Value, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Toggles the completion status of a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <returns>The updated task.</returns>
    [HttpPatch("{id:int}/toggle-complete")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> ToggleComplete(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.ToggleCompleteAsync(id, userId.Value, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Updates the sort order of multiple tasks (for drag-and-drop reordering).
    /// </summary>
    /// <param name="request">The sort order updates.</param>
    [HttpPatch("sort-order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSortOrder(
        [FromBody] UpdateSortOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var sortOrders = request.Items
            .Select(x => (x.TaskId, x.SortOrder))
            .ToList();

        var result = await _taskService.UpdateSortOrderAsync(userId.Value, sortOrders, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Reorders tasks based on the provided array of task IDs (for drag-and-drop).
    /// The sort order is determined by the position in the array.
    /// </summary>
    /// <param name="request">The reorder request containing task IDs in desired order.</param>
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.ReorderTasksAsync(userId.Value, request.TaskIds, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

/// <summary>
/// Request DTO for updating task sort orders.
/// </summary>
public sealed record UpdateSortOrderRequest
{
    public required IReadOnlyList<SortOrderItem> Items { get; init; }
}

/// <summary>
/// Individual sort order update item.
/// </summary>
public sealed record SortOrderItem
{
    public int TaskId { get; init; }
    public int SortOrder { get; init; }
}

/// <summary>
/// Request DTO for reordering tasks by providing task IDs in the desired order.
/// </summary>
public sealed record ReorderRequest
{
    public required IReadOnlyList<int> TaskIds { get; init; }
}
