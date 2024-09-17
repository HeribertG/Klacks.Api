using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Repositories
{
  public class BaseRepository<TEntity> : IBaseRepository<TEntity>
      where TEntity : BaseEntity
  {
    private readonly DataBaseContext context;

    public BaseRepository(DataBaseContext context)
    {
      this.context = context;
    }

    public void Add(TEntity model)
    {
      this.context.Set<TEntity>().Add(model);
    }

    public async Task<TEntity?> Delete(Guid id)
    {
      var entity = await this.context.Set<TEntity>().FirstOrDefaultAsync(add => add.Id == id);
      this.context.Remove<TEntity>(entity!);
      return entity!;
    }

    public async Task<bool> Exists(Guid id)
    {
      return await this.context.Set<TEntity>().AnyAsync(e => e.Id == id);
    }

    public async Task<TEntity?> Get(Guid id)
    {
      return await this.context.Set<TEntity>().FirstOrDefaultAsync(add => add.Id == id);
    }

    public async Task<List<TEntity>> List()
    {
      return await this.context.Set<TEntity>().ToListAsync();
    }

    public TEntity Put(TEntity model)
    {
      this.context.Set<TEntity>().Update(model);

      return model;
    }

    public void Remove(TEntity model)
    {
      this.context.Set<TEntity>().Remove(model);
    }
  }
}
