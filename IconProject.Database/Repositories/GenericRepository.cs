using Microsoft.EntityFrameworkCore;
using IconProject.Database.Repositories.Interfaces;
using System.Linq.Expressions;

namespace IconProject.Database.Repositories;

public class GenericRepository<TEntity>(ApplicationDbContext context) : IGenericRepository<TEntity>
    where TEntity : class
{
    protected readonly ApplicationDbContext Context = context;
    protected readonly DbSet<TEntity> Entities = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(int id, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = Entities;

        foreach (var include in includes)
            query = query.Include(include);

        return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync()
    {
        return await Entities.ToListAsync();
    }

    public async Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPaginatedAsync(
        int skip,
        int take,
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = Entities;

        if (filter != null)
            query = query.Where(filter);

        foreach (var include in includes)
            query = query.Include(include);

        int totalCount = await query.CountAsync();
        List<TEntity> items = await query
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await Entities.Where(predicate).ToListAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await Entities.AnyAsync(predicate);
    }

    public async Task AddAsync(TEntity entity)
    {
        await Entities.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await Entities.AddRangeAsync(entities);
    }
    
    public Task SaveChangesAsync() => Context.SaveChangesAsync();

    public void Remove(TEntity entity)
    {
        Entities.Remove(entity);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        Entities.RemoveRange(entities);
    }
}