using IconProject.Database.Models;
using IconProject.Database.Repositories.Interfaces;

namespace IconProject.Database.UnitOfWork;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<TaskEntity> Tasks { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
