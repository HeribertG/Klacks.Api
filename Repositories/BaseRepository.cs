using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : BaseEntity
    {
        protected readonly ILogger<TEntity> Logger;
        private readonly DataBaseContext context;

        public BaseRepository(DataBaseContext context, ILogger<TEntity> logger)
        {
            this.context = context;
            this.Logger = logger;
        }

        public virtual async Task Add(TEntity model)
        {
            this.context.Set<TEntity>().Add(model);
        }

        public async virtual Task<TEntity?> Delete(Guid id)
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

        public virtual async Task<TEntity?> Put(TEntity model)
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