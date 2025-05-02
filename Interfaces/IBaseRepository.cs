using Klacks.Api.Datas;

namespace Klacks.Api.Interfaces;

public interface IBaseRepository<TEntity>
    where TEntity : BaseEntity
{
    Task Add(TEntity model);

    Task<TEntity?> Delete(Guid id);

    Task<bool> Exists(Guid id);

    Task<TEntity?> Get(Guid id);

    Task<List<TEntity>> List();

    Task<TEntity?> Put(TEntity model);

    void Remove(TEntity model);
}
