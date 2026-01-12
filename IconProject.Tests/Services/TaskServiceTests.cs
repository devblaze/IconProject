using FluentAssertions;
using IconProject.Database.Models;
using IconProject.Dtos.Task;
using IconProject.Services;
using IconProject.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IconProject.Tests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly Mock<ILogger<TaskService>> _loggerMock;

    public TaskServiceTests()
    {
        _loggerMock = new Mock<ILogger<TaskService>>();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region GetAllByUserIdAsync Tests

    [Fact]
    public async Task GetAllByUserIdAsync_WithExistingTasks_ReturnsAllUserTasks()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user.Id, 3);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetAllByUserIdAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().AllSatisfy(t => t.UserId.Should().Be(user.Id));
    }

    [Fact]
    public async Task GetAllByUserIdAsync_WithNoTasks_ReturnsEmptyList()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetAllByUserIdAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllByUserIdAsync_DoesNotReturnOtherUsersTasks()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user1 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user1@test.com");
        var user2 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user2@test.com");
        await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user1.Id, 2);
        await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user2.Id, 3);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetAllByUserIdAsync(user1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(t => t.UserId.Should().Be(user1.Id));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingTask_ReturnsTask()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user.Id, 1);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetByIdAsync(tasks[0].Id, user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(tasks[0].Id);
        result.Value.Title.Should().Be(tasks[0].Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentTask_ReturnsNotFoundError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetByIdAsync(999, user.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUsersTask_ReturnsNotOwnedError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user1 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user1@test.com");
        var user2 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user2@test.com");
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user1.Id, 1);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetByIdAsync(tasks[0].Id, user2.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotOwned");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndReturnsTask()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);
        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Task Description",
            Priority = Priority.High
        };

        // Act
        var result = await service.CreateAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("New Task");
        result.Value.Description.Should().Be("Task Description");
        result.Value.Priority.Should().Be(Priority.High);
        result.Value.IsComplete.Should().BeFalse();
        result.Value.UserId.Should().Be(user.Id);

        // Verify persisted
        var savedTask = await context.Tasks.FindAsync(result.Value.Id);
        savedTask.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentUser_ReturnsNotFoundError()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new TaskService(unitOfWork, _loggerMock.Object);
        var request = new CreateTaskRequest { Title = "New Task" };

        // Act
        var result = await service.CreateAsync(999, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task CreateAsync_WithDefaultPriority_UsesMedium()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);
        var request = new CreateTaskRequest { Title = "New Task" };

        // Act
        var result = await service.CreateAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Priority.Should().Be(Priority.Medium);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesAndReturnsTask()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user.Id, 1);
        var service = new TaskService(unitOfWork, _loggerMock.Object);
        var request = new UpdateTaskRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            IsComplete = true,
            Priority = Priority.High,
            SortOrder = 10
        };

        // Act
        var result = await service.UpdateAsync(tasks[0].Id, user.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Updated Title");
        result.Value.Description.Should().Be("Updated Description");
        result.Value.IsComplete.Should().BeTrue();
        result.Value.Priority.Should().Be(Priority.High);
        result.Value.SortOrder.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentTask_ReturnsNotFoundError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);
        var request = new UpdateTaskRequest
        {
            Title = "Updated",
            IsComplete = false,
            Priority = Priority.Low
        };

        // Act
        var result = await service.UpdateAsync(999, user.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task UpdateAsync_WithOtherUsersTask_ReturnsNotOwnedError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user1 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user1@test.com");
        var user2 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user2@test.com");
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user1.Id, 1);
        var service = new TaskService(unitOfWork, _loggerMock.Object);
        var request = new UpdateTaskRequest
        {
            Title = "Updated",
            IsComplete = false,
            Priority = Priority.Low
        };

        // Act
        var result = await service.UpdateAsync(tasks[0].Id, user2.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotOwned");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingTask_DeletesTask()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user.Id, 1);
        var taskId = tasks[0].Id;
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.DeleteAsync(taskId, user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify deleted
        var deletedTask = await context.Tasks.FindAsync(taskId);
        deletedTask.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentTask_ReturnsNotFoundError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.DeleteAsync(999, user.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task DeleteAsync_WithOtherUsersTask_ReturnsNotOwnedError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user1 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user1@test.com");
        var user2 = await MockUnitOfWorkFactory.SeedTestUserAsync(context, "user2@test.com");
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user1.Id, 1);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.DeleteAsync(tasks[0].Id, user2.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotOwned");

        // Verify not deleted
        var task = await context.Tasks.FindAsync(tasks[0].Id);
        task.Should().NotBeNull();
    }

    #endregion

    #region ToggleCompleteAsync Tests

    [Fact]
    public async Task ToggleCompleteAsync_WithIncompleteTask_MarksAsComplete()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var task = new TaskEntity
        {
            Title = "Test Task",
            IsComplete = false,
            Priority = Priority.Medium,
            UserId = user.Id
        };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.ToggleCompleteAsync(task.Id, user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleCompleteAsync_WithCompleteTask_MarksAsIncomplete()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var task = new TaskEntity
        {
            Title = "Test Task",
            IsComplete = true,
            Priority = Priority.Medium,
            UserId = user.Id
        };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.ToggleCompleteAsync(task.Id, user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleCompleteAsync_WithNonExistentTask_ReturnsNotFoundError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.ToggleCompleteAsync(999, user.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region ReorderTasksAsync Tests

    [Fact]
    public async Task ReorderTasksAsync_WithValidTaskIds_UpdatesSortOrder()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var tasks = await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user.Id, 3);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Reverse the order
        var reorderedIds = tasks.Select(t => t.Id).Reverse().ToList();

        // Act
        var result = await service.ReorderTasksAsync(user.Id, reorderedIds);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify sort orders updated
        foreach (var (taskId, expectedSortOrder) in reorderedIds.Select((id, index) => (id, index)))
        {
            var task = await context.Tasks.FindAsync(taskId);
            task!.SortOrder.Should().Be(expectedSortOrder);
        }
    }

    [Fact]
    public async Task ReorderTasksAsync_WithEmptyList_ReturnsSuccess()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.ReorderTasksAsync(user.Id, new List<int>());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReorderTasksAsync_WithNonExistentTask_ReturnsNotFoundError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.ReorderTasksAsync(user.Id, new List<int> { 999, 998 });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region GetPaginatedAsync Tests

    [Fact]
    public async Task GetPaginatedAsync_ReturnsCorrectPage()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        await MockUnitOfWorkFactory.SeedTestTasksAsync(context, user.Id, 10);
        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetPaginatedAsync(user.Id, page: 1, pageSize: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(10);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaginatedAsync_WithIsCompleteFilter_FiltersCorrectly()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);

        // Create specific tasks with known completion status
        context.Tasks.AddRange(
            new TaskEntity { Title = "Complete 1", IsComplete = true, UserId = user.Id, Priority = Priority.Low },
            new TaskEntity { Title = "Complete 2", IsComplete = true, UserId = user.Id, Priority = Priority.Low },
            new TaskEntity { Title = "Incomplete 1", IsComplete = false, UserId = user.Id, Priority = Priority.Low }
        );
        await context.SaveChangesAsync();

        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetPaginatedAsync(user.Id, page: 1, pageSize: 10, isComplete: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(t => t.IsComplete.Should().BeTrue());
    }

    #endregion
}
