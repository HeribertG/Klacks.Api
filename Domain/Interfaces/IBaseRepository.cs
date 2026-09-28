// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces;

public interface IBaseRepository<TEntity>
    where TEntity : BaseEntity
{
    Task Add(TEntity model);

    Task<TEntity?> Delete(Guid id);

    void Detach(TEntity model);

    Task<bool> Exists(Guid id);

    Task<TEntity?> Get(Guid id);

    Task<TEntity?> GetNoTracking(Guid id);

    Task<List<TEntity>> List();

    Task<TEntity?> Put(TEntity model);

    void Remove(TEntity model);
}
