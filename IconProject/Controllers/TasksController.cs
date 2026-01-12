using IconProject.Dtos.Task;
using IconProject.Extensions;
using IconProject.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IconProject.Controllers;

/// <summary>
/// API controller for task management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Gets all tasks for the current user.
    /// </summary>
    /// <param name="userId">The user ID (will be from auth in production).</param>
    /// <returns>A list of tasks.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(
        [FromHeader(Name = "X-User-Id")] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetAllByUserIdAsync(userId, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Gets paginated tasks for the current user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 100).</param>
    /// <param name="isComplete">Filter by completion status.</param>
    /// <returns>Paginated task results.</returns>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PaginatedTaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedTaskResponse>> GetPaginated(
        [FromHeader(Name = "X-User-Id")] int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isComplete = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _taskService.GetPaginatedAsync(userId, page, pageSize, isComplete, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Gets a specific task by ID.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The task details.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TaskResponse>> GetById(
        int id,
        [FromHeader(Name = "X-User-Id")] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetByIdAsync(id, userId, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="request">The task creation request.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The created task.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        [FromHeader(Name = "X-User-Id")] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateAsync(userId, request, cancellationToken);

        return result.Match(
            onSuccess: task => CreatedAtAction(
                nameof(GetById),
                new { id = task.Id },
                task),
            onFailure: error => result.ToActionResult(Request.Path));
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The updated task.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Update(
        int id,
        [FromBody] UpdateTaskRequest request,
        [FromHeader(Name = "X-User-Id")] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateAsync(id, userId, request, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="userId">The user ID.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        int id,
        [FromHeader(Name = "X-User-Id")] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteAsync(id, userId, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Toggles the completion status of a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The updated task.</returns>
    [HttpPatch("{id:int}/toggle-complete")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TaskResponse>> ToggleComplete(
        int id,
        [FromHeader(Name = "X-User-Id")] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.ToggleCompleteAsync(id, userId, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    /// <summary>
    /// Updates the sort order of multiple tasks (for drag-and-drop reordering).
    /// </summary>
    /// <param name="request">The sort order updates.</param>
    /// <param name="userId">The user ID.</param>
    [HttpPatch("sort-order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSortOrder(
        [FromBody] UpdateSortOrderRequest request,
        [FromHeader(Name = "X-User-Id")] int userId,
        CancellationToken cancellationToken)
    {
        var sortOrders = request.Items
            .Select(x => (x.TaskId, x.SortOrder))
            .ToList();

        var result = await _taskService.UpdateSortOrderAsync(userId, sortOrders, cancellationToken);
        return result.ToActionResult(Request.Path);
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
