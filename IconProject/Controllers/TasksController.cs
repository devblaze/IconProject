using IconProject.Common.Dtos.Requests.Task;
using IconProject.Common.Dtos.Responses.Task;
using IconProject.Common.Enums;
using IconProject.Extensions;
using IconProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IconProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TasksController : AuthorizedControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(
        [FromQuery] bool? isComplete = null,
        [FromQuery] Priority? priority = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _taskService.GetAllByUserIdAsync(GetUserId(), isComplete, priority, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PaginatedTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedTaskResponse>> GetPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isComplete = null,
        [FromQuery] Priority? priority = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _taskService.GetPaginatedAsync(GetUserId(), page, pageSize, isComplete, priority, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetByIdAsync(id, GetUserId(), cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateAsync(GetUserId(), request, cancellationToken);

        return result.Match(
            onSuccess: task => CreatedAtAction(
                nameof(GetById),
                new { id = task.Id },
                task),
            onFailure: _ => result.ToActionResult(Request.Path));
    }

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
        var result = await _taskService.UpdateAsync(id, GetUserId(), request, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteAsync(id, GetUserId(), cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    [HttpPatch("{id:int}/toggle-complete")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> ToggleComplete(int id, CancellationToken cancellationToken)
    {
        var result = await _taskService.ToggleCompleteAsync(id, GetUserId(), cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    [HttpPatch("sort-order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSortOrder(
        [FromBody] UpdateSortOrderRequest request,
        CancellationToken cancellationToken)
    {
        var sortOrders = request.Items
            .Select(x => (x.TaskId, x.SortOrder))
            .ToList();

        var result = await _taskService.UpdateSortOrderAsync(GetUserId(), sortOrders, cancellationToken);
        return result.ToActionResult(Request.Path);
    }

    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.ReorderTasksAsync(GetUserId(), request.TaskIds, cancellationToken);
        return result.ToActionResult(Request.Path);
    }
}
