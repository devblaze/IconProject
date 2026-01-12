using IconProject.Database.Models;
using IconProject.Database.Repositories.Interfaces;

namespace IconProject.Database.UnitOfWork;

/// <summary>
/// Represents a unit of work that coordinates changes across multiple repositories
/// and ensures they are committed as a single transaction.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the repository for User entities.
    /// </summary>
    IGenericRepository<User> Users { get; }

    /// <summary>
    /// Gets the repository for TaskEntity entities.
    /// </summary>
    IGenericRepository<TaskEntity> Tasks { get; }

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
