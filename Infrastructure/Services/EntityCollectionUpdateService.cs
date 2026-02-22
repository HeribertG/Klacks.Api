// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class EntityCollectionUpdateService
{
    private readonly DataBaseContext _context;

    public EntityCollectionUpdateService(DataBaseContext context)
    {
        _context = context;
    }

    public void UpdateCollection<TEntity>(
        ICollection<TEntity> existingCollection,
        ICollection<TEntity> updatedCollection,
        Guid parentId,
        Action<TEntity, Guid> setParentId)
        where TEntity : BaseEntity
    {
        var existingList = existingCollection.ToList();
        var updatedList = updatedCollection.ToList();

        foreach (var existingEntity in existingList)
        {
            var updatedEntity = updatedList.FirstOrDefault(e => e.Id == existingEntity.Id);
            if (updatedEntity == null)
            {
                _context.Entry(existingEntity).State = EntityState.Deleted;
            }
            else
            {
                var entry = _context.Entry(existingEntity);
                entry.CurrentValues.SetValues(updatedEntity);
                entry.State = EntityState.Modified;
            }
        }

        foreach (var newEntity in updatedList.Where(e => e.Id == Guid.Empty || !existingList.Any(ex => ex.Id == e.Id)))
        {
            setParentId(newEntity, parentId);
            _context.Entry(newEntity).State = EntityState.Added;
            existingCollection.Add(newEntity);
        }
    }

    public void UpdateSingleEntity<TEntity>(
        TEntity? existingEntity,
        TEntity? updatedEntity,
        Guid parentId,
        Action<TEntity, Guid> setParentId,
        Action<TEntity> setExistingEntity,
        Action clearExistingEntity)
        where TEntity : BaseEntity
    {
        if (updatedEntity != null)
        {
            if (existingEntity == null)
            {
                setParentId(updatedEntity, parentId);
                _context.Entry(updatedEntity).State = EntityState.Added;
                setExistingEntity(updatedEntity);
            }
            else
            {
                var existingId = existingEntity.Id;
                var entry = _context.Entry(existingEntity);
                entry.CurrentValues.SetValues(updatedEntity);
                existingEntity.Id = existingId;
                entry.State = EntityState.Modified;
            }
        }
        else if (existingEntity != null)
        {
            _context.Entry(existingEntity).State = EntityState.Deleted;
            clearExistingEntity();
        }
    }
}
