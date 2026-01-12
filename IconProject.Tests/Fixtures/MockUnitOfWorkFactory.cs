using IconProject.Database;
using IconProject.Database.Models;
using IconProject.Database.Repositories;
using IconProject.Database.UnitOfWork;

namespace IconProject.Tests.Fixtures;

/// <summary>
/// Factory for creating real UnitOfWork instances backed by in-memory database for testing.
/// </summary>
public static class MockUnitOfWorkFactory
{
    /// <summary>
    /// Creates a real UnitOfWork with an in-memory database.
    /// </summary>
    public static (IUnitOfWork UnitOfWork, ApplicationDbContext Context) Create(string? databaseName = null)
    {
        var context = TestDbContextFactory.Create(databaseName);
        var unitOfWork = new UnitOfWork(context);
        return (unitOfWork, context);
    }

    /// <summary>
    /// Creates a real UnitOfWork with seeded test data.
    /// </summary>
    public static (IUnitOfWork UnitOfWork, ApplicationDbContext Context) CreateWithSeedData(string? databaseName = null)
    {
        var context = TestDbContextFactory.CreateWithSeedData(databaseName);
        var unitOfWork = new UnitOfWork(context);
        return (unitOfWork, context);
    }

    /// <summary>
    /// Seeds a test user and returns the user entity.
    /// </summary>
    public static async Task<User> SeedTestUserAsync(ApplicationDbContext context, string email = "test@example.com")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hashedpassword123",
            FirstName = "Test",
            LastName = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Seeds test tasks for a user.
    /// </summary>
    public static async Task<List<TaskEntity>> SeedTestTasksAsync(
        ApplicationDbContext context,
        int userId,
        int count = 3)
    {
        var tasks = new List<TaskEntity>();

        for (int i = 1; i <= count; i++)
        {
            var task = new TaskEntity
            {
                Title = $"Test Task {i}",
                Description = $"Description for task {i}",
                IsComplete = i % 2 == 0,
                Priority = (Priority)(i % 3),
                SortOrder = i,
                UserId = userId
            };
            tasks.Add(task);
        }

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
        return tasks;
    }
}
