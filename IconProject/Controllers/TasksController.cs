using System.Security.Claims;
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
public class TasksController : ControllerBase
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
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.GetAllByUserIdAsync(userId.Value, isComplete, priority, cancellationToken);
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
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.GetPaginatedAsync(userId.Value, page, pageSize, isComplete, priority, cancellationToken);
        return result.ToActionResult(Request.Path);
    }
    
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