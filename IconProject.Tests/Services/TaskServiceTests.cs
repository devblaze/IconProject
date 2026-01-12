using Shouldly;
using IconProject.Common.Dtos.Requests.Task;
using IconProject.Common.Dtos.Responses.Task;
using IconProject.Common.Enums;
using IconProject.Database.Models;
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(3);
        result.Value.ShouldAllBe(t => t.UserId == user.Id);
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldAllBe(t => t.UserId == user1.Id);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_WithIsCompleteFilter_ReturnsOnlyCompletedTasks()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);

        context.Tasks.AddRange(
            new TaskEntity { Title = "Complete 1", IsComplete = true, UserId = user.Id, Priority = Priority.Low },
            new TaskEntity { Title = "Complete 2", IsComplete = true, UserId = user.Id, Priority = Priority.Low },
            new TaskEntity { Title = "Incomplete 1", IsComplete = false, UserId = user.Id, Priority = Priority.Low }
        );
        await context.SaveChangesAsync();

        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetAllByUserIdAsync(user.Id, isComplete: true);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldAllBe(t => t.IsComplete);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_WithIsCompleteFilterFalse_ReturnsOnlyIncompleteTasks()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);

        context.Tasks.AddRange(
            new TaskEntity { Title = "Complete 1", IsComplete = true, UserId = user.Id, Priority = Priority.Low },
            new TaskEntity { Title = "Incomplete 1", IsComplete = false, UserId = user.Id, Priority = Priority.Low },
            new TaskEntity { Title = "Incomplete 2", IsComplete = false, UserId = user.Id, Priority = Priority.Low }
        );
        await context.SaveChangesAsync();

        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetAllByUserIdAsync(user.Id, isComplete: false);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldAllBe(t => !t.IsComplete);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_WithPriorityFilter_ReturnsOnlyMatchingPriorityTasks()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);

        context.Tasks.AddRange(
            new TaskEntity { Title = "High 1", IsComplete = false, UserId = user.Id, Priority = Priority.High },
            new TaskEntity { Title = "High 2", IsComplete = false, UserId = user.Id, Priority = Priority.High },
            new TaskEntity { Title = "Medium 1", IsComplete = false, UserId = user.Id, Priority = Priority.Medium },
            new TaskEntity { Title = "Low 1", IsComplete = false, UserId = user.Id, Priority = Priority.Low }
        );
        await context.SaveChangesAsync();

        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetAllByUserIdAsync(user.Id, priority: Priority.High);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldAllBe(t => t.Priority == Priority.High);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_WithBothFilters_ReturnsMatchingTasks()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);

        context.Tasks.AddRange(
            new TaskEntity { Title = "High Complete", IsComplete = true, UserId = user.Id, Priority = Priority.High },
            new TaskEntity { Title = "High Incomplete", IsComplete = false, UserId = user.Id, Priority = Priority.High },
            new TaskEntity { Title = "Low Complete", IsComplete = true, UserId = user.Id, Priority = Priority.Low }
        );
        await context.SaveChangesAsync();

        var service = new TaskService(unitOfWork, _loggerMock.Object);

        // Act
        var result = await service.GetAllByUserIdAsync(user.Id, isComplete: true, priority: Priority.High);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
        result.Value[0].Title.ShouldBe("High Complete");
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(tasks[0].Id);
        result.Value.Title.ShouldBe(tasks[0].Title);
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotOwned");
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Title.ShouldBe("New Task");
        result.Value.Description.ShouldBe("Task Description");
        result.Value.Priority.ShouldBe(Priority.High);
        result.Value.IsComplete.ShouldBeFalse();
        result.Value.UserId.ShouldBe(user.Id);

        // Verify persisted
        var savedTask = await context.Tasks.FindAsync(result.Value.Id);
        savedTask.ShouldNotBeNull();
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Priority.ShouldBe(Priority.Medium);
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Title.ShouldBe("Updated Title");
        result.Value.Description.ShouldBe("Updated Description");
        result.Value.IsComplete.ShouldBeTrue();
        result.Value.Priority.ShouldBe(Priority.High);
        result.Value.SortOrder.ShouldBe(10);
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotOwned");
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
        result.IsSuccess.ShouldBeTrue();

        // Verify deleted
        var deletedTask = await context.Tasks.FindAsync(taskId);
        deletedTask.ShouldBeNull();
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotOwned");

        // Verify not deleted
        var task = await context.Tasks.FindAsync(tasks[0].Id);
        task.ShouldNotBeNull();
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsComplete.ShouldBeTrue();
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsComplete.ShouldBeFalse();
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
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
        result.IsSuccess.ShouldBeTrue();

        // Verify sort orders updated
        foreach (var (taskId, expectedSortOrder) in reorderedIds.Select((id, index) => (id, index)))
        {
            var task = await context.Tasks.FindAsync(taskId);
            task!.SortOrder.ShouldBe(expectedSortOrder);
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
        result.IsSuccess.ShouldBeTrue();
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(5);
        result.Value.TotalCount.ShouldBe(10);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(5);
        result.Value.TotalPages.ShouldBe(2);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.HasPreviousPage.ShouldBeFalse();
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items.ShouldAllBe(t => t.IsComplete);
    }

    #endregion
}
