using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces;

public interface IBaseRepository<TEntity>
    where TEntity : BaseEntity
{
    Task Add(TEntity model);

    Task<TEntity?> Delete(Guid id);

    Task<bool> Exists(Guid id);

    Task<TEntity?> Get(Guid id);

    Task<TEntity?> GetNoTracking(Guid id);

    Task<List<TEntity>> List();

    Task<TEntity?> Put(TEntity model);

    void Remove(TEntity model);
}
