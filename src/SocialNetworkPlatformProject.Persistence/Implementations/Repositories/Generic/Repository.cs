using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Persistence.Contexts;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

public class Repository<T> : IRepository<T> where T : BaseEntity, new()
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> GetAll(
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = false,
        bool asNoTracking = false,
        int page = 0,
        int take = 0,
        params string[]? includes)
    {
        IQueryable<T> query = _dbSet;

        if (asNoTracking)
            query = query.AsNoTracking();

        if (filter != null)
            query = query.Where(filter);

        if (includes != null && includes.Length > 0)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        if (orderBy != null)
        {
            query = isDescending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        }

        if (page > 0 && take > 0)
            query = query.Skip((page - 1) * take).Take(take);

        return query;
    }

    public async Task<T?> GetByIdAsync(Guid id, params string[] includes)
    {
        IQueryable<T> query = _dbSet;

        if (includes != null && includes.Length > 0)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> filter)
    {
        return await _dbSet.AnyAsync(filter);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
