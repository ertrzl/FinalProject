using System.Linq.Expressions;
using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;

public interface IRepository<T> where T : BaseEntity, new()
{
    IQueryable<T> GetAll(
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = false,
        bool asNoTracking = false,
        int page = 0,
        int take = 0,
        params string[]? includes
    );

    Task<T?> GetByIdAsync(Guid id, params string[] includes);

    Task AddAsync(T entity);

    void Update(T entity);

    void Delete(T entity);

    Task<bool> AnyAsync(Expression<Func<T, bool>> filter);

    Task SaveChangesAsync();
}
