using System.Security.Claims;
using Shouldly;
using IconProject.Common.Dtos;
using IconProject.Common.Dtos.Requests.Task;
using IconProject.Common.Dtos.Responses.Task;
using IconProject.Common.Enums;
using IconProject.Controllers;
using IconProject.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IconProject.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _taskServiceMock;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _taskServiceMock = new Mock<ITaskService>();
        _controller = new TasksController(_taskServiceMock.Object);
        SetupUserContext(1);
    }

    private void SetupUserContext(int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        httpContext.Request.Path = "/api/tasks";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupUnauthenticatedContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/tasks";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithAuthenticatedUser_ReturnsOkWithTasks()
    {
        // Arrange
        var tasks = new List<TaskResponse>
        {
            new() { Id = 1, Title = "Task 1", UserId = 1, Priority = Priority.Low },
            new() { Id = 2, Title = "Task 2", UserId = 1, Priority = Priority.High }
        };
        _taskServiceMock
            .Setup(x => x.GetAllByUserIdAsync(1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskResponse>>.Success(tasks));

        // Act
        var result = await _controller.GetAll(null, null, CancellationToken.None);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedTasks = okResult.Value.ShouldBeAssignableTo<IReadOnlyList<TaskResponse>>();
        returnedTasks.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAll_WithIsCompleteFilter_PassesFilterToService()
    {
        // Arrange
        var tasks = new List<TaskResponse>
        {
            new() { Id = 1, Title = "Task 1", UserId = 1, Priority = Priority.Low, IsComplete = true }
        };
        _taskServiceMock
            .Setup(x => x.GetAllByUserIdAsync(1, true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskResponse>>.Success(tasks));

        // Act
        var result = await _controller.GetAll(true, null, CancellationToken.None);

        // Assert
        _taskServiceMock.Verify(x => x.GetAllByUserIdAsync(1, true, null, It.IsAny<CancellationToken>()), Times.Once);
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedTasks = okResult.Value.ShouldBeAssignableTo<IReadOnlyList<TaskResponse>>();
        returnedTasks.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAll_WithUnauthenticatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupUnauthenticatedContext();

        // Act & Assert
        // Note: In production, the [Authorize] attribute prevents unauthenticated access.
        // This test verifies the fallback behavior when claims are missing.
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _controller.GetAll(null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_WithPriorityFilter_PassesFilterToService()
    {
        // Arrange
        var tasks = new List<TaskResponse>
        {
            new() { Id = 1, Title = "Task 1", UserId = 1, Priority = Priority.High }
        };
        _taskServiceMock
            .Setup(x => x.GetAllByUserIdAsync(1, null, Priority.High, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskResponse>>.Success(tasks));

        // Act
        var result = await _controller.GetAll(null, Priority.High, CancellationToken.None);

        // Assert
        _taskServiceMock.Verify(x => x.GetAllByUserIdAsync(1, null, Priority.High, It.IsAny<CancellationToken>()), Times.Once);
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedTasks = okResult.Value.ShouldBeAssignableTo<IReadOnlyList<TaskResponse>>();
        returnedTasks.Count.ShouldBe(1);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingTask_ReturnsOkWithTask()
    {
        // Arrange
        var task = new TaskResponse
        {
            Id = 1,
            Title = "Test Task",
            UserId = 1,
            Priority = Priority.Medium
        };
        _taskServiceMock
            .Setup(x => x.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskResponse>.Success(task));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedTask = okResult.Value.ShouldBeOfType<TaskResponse>();
        returnedTask.Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetById_WithNonExistentTask_ReturnsNotFound()
    {
        // Arrange
        _taskServiceMock
            .Setup(x => x.GetByIdAsync(999, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskResponse>.Failure(Error.NotFound("Task", 999)));

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task GetById_WithOtherUsersTask_ReturnsForbidden()
    {
        // Arrange
        _taskServiceMock
            .Setup(x => x.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskResponse>.Failure(DomainErrors.Task.NotOwned));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(403);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedWithTask()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Description",
            Priority = Priority.High
        };
        var createdTask = new TaskResponse
        {
            Id = 1,
            Title = "New Task",
            Description = "Description",
            Priority = Priority.High,
            UserId = 1
        };
        _taskServiceMock
            .Setup(x => x.CreateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskResponse>.Success(createdTask));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.ActionName.ShouldBe(nameof(TasksController.GetById));
        var returnedTask = createdResult.Value.ShouldBeOfType<TaskResponse>();
        returnedTask.Title.ShouldBe("New Task");
    }

    [Fact]
    public async Task Create_WithUnauthenticatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupUnauthenticatedContext();
        var request = new CreateTaskRequest { Title = "New Task" };

        // Act & Assert
        // Note: In production, the [Authorize] attribute prevents unauthenticated access.
        // This test verifies the fallback behavior when claims are missing.
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _controller.Create(request, CancellationToken.None));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOkWithUpdatedTask()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = "Updated Task",
            IsComplete = true,
            Priority = Priority.Low
        };
        var updatedTask = new TaskResponse
        {
            Id = 1,
            Title = "Updated Task",
            IsComplete = true,
            Priority = Priority.Low,
            UserId = 1
        };
        _taskServiceMock
            .Setup(x => x.UpdateAsync(1, 1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskResponse>.Success(updatedTask));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedTask = okResult.Value.ShouldBeOfType<TaskResponse>();
        returnedTask.Title.ShouldBe("Updated Task");
        returnedTask.IsComplete.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_WithNonExistentTask_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateTaskRequest
        {
            Title = "Updated",
            IsComplete = false,
            Priority = Priority.Low
        };
        _taskServiceMock
            .Setup(x => x.UpdateAsync(999, 1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskResponse>.Failure(Error.NotFound("Task", 999)));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(404);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingTask_ReturnsNoContent()
    {
        // Arrange
        _taskServiceMock
            .Setup(x => x.DeleteAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistentTask_ReturnsNotFound()
    {
        // Arrange
        _taskServiceMock
            .Setup(x => x.DeleteAsync(999, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.NotFound("Task", 999)));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task Delete_WithOtherUsersTask_ReturnsForbidden()
    {
        // Arrange
        _taskServiceMock
            .Setup(x => x.DeleteAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(DomainErrors.Task.NotOwned));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(403);
    }

    #endregion

    #region ToggleComplete Tests

    [Fact]
    public async Task ToggleComplete_WithExistingTask_ReturnsOkWithToggledTask()
    {
        // Arrange
        var toggledTask = new TaskResponse
        {
            Id = 1,
            Title = "Task",
            IsComplete = true,
            UserId = 1,
            Priority = Priority.Medium
        };
        _taskServiceMock
            .Setup(x => x.ToggleCompleteAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskResponse>.Success(toggledTask));

        // Act
        var result = await _controller.ToggleComplete(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedTask = okResult.Value.ShouldBeOfType<TaskResponse>();
        returnedTask.IsComplete.ShouldBeTrue();
    }

    #endregion

    #region Reorder Tests

    [Fact]
    public async Task Reorder_WithValidTaskIds_ReturnsNoContent()
    {
        // Arrange
        var request = new ReorderRequest { TaskIds = new List<int> { 3, 1, 2 } };
        _taskServiceMock
            .Setup(x => x.ReorderTasksAsync(1, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Reorder(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Reorder_WithNonExistentTask_ReturnsNotFound()
    {
        // Arrange
        var request = new ReorderRequest { TaskIds = new List<int> { 999 } };
        _taskServiceMock
            .Setup(x => x.ReorderTasksAsync(1, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.NotFound("Task", 999)));

        // Act
        var result = await _controller.Reorder(request, CancellationToken.None);

        // Assert
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(404);
    }

    #endregion

    #region GetPaginated Tests

    [Fact]
    public async Task GetPaginated_ReturnsOkWithPaginatedResults()
    {
        // Arrange
        var paginatedResponse = new PaginatedTaskResponse
        {
            Items = new List<TaskResponse>
            {
                new() { Id = 1, Title = "Task 1", UserId = 1, Priority = Priority.Low }
            },
            TotalCount = 10,
            Page = 1,
            PageSize = 5
        };
        _taskServiceMock
            .Setup(x => x.GetPaginatedAsync(1, 1, 5, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginatedTaskResponse>.Success(paginatedResponse));

        // Act
        var result = await _controller.GetPaginated(1, 5, null, null, CancellationToken.None);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<PaginatedTaskResponse>();
        response.Items.Count.ShouldBe(1);
        response.TotalCount.ShouldBe(10);
    }

    [Fact]
    public async Task GetPaginated_WithIsCompleteFilter_PassesFilterToService()
    {
        // Arrange
        _taskServiceMock
            .Setup(x => x.GetPaginatedAsync(1, 1, 10, true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginatedTaskResponse>.Success(new PaginatedTaskResponse
            {
                Items = new List<TaskResponse>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            }));

        // Act
        await _controller.GetPaginated(1, 10, true, null, CancellationToken.None);

        // Assert
        _taskServiceMock.Verify(x => x.GetPaginatedAsync(1, 1, 10, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
