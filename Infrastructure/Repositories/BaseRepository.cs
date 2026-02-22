// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : BaseEntity
    {
        protected readonly ILogger<TEntity> Logger;
        protected readonly DataBaseContext context;

        public BaseRepository(DataBaseContext context, ILogger<TEntity> logger)
        {
            this.context = context;
            this.Logger = logger;
        }

        public virtual async Task Add(TEntity model)
        {
            await this.context.Set<TEntity>().AddAsync(model);
        }

        public async virtual Task<TEntity?> Delete(Guid id)
        {
            var entity = await this.context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null) return null;

            this.context.Remove(entity);
            return entity;
        }

        public async Task<bool> Exists(Guid id)
        {
            return await this.context.Set<TEntity>().AnyAsync(e => e.Id == id);
        }

        public virtual async Task<TEntity?> Get(Guid id)
        {
            return await this.context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
        }

        public virtual async Task<TEntity?> GetNoTracking(Guid id)
        {
            return await this.context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        }

        public virtual async Task<List<TEntity>> List()
        {
            return await this.context.Set<TEntity>().ToListAsync();
        }

        public virtual Task<TEntity?> Put(TEntity model)
        {
            this.context.Set<TEntity>().Update(model);
            return Task.FromResult<TEntity?>(model);
        }

        public void Remove(TEntity model)
        {
            this.context.Set<TEntity>().Remove(model);
        }
    }
}