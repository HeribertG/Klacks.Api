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
        UpdateCollection(existingCollection, updatedCollection, parentId, setParentId, e => (object)e.Id);
    }

    public void UpdateCollection<TEntity, TKey>(
        ICollection<TEntity> existingCollection,
        ICollection<TEntity> updatedCollection,
        Guid parentId,
        Action<TEntity, Guid> setParentId,
        Func<TEntity, TKey> keySelector)
        where TEntity : BaseEntity
    {
        var existingList = existingCollection.ToList();
        var updatedList = updatedCollection.ToList();
        var emptyIdSentinel = (object)Guid.Empty;

        foreach (var existingEntity in existingList)
        {
            var existingKey = (object?)keySelector(existingEntity);
            var updatedEntity = updatedList.FirstOrDefault(e =>
            {
                var candidateKey = (object?)keySelector(e);
                if (candidateKey is null || candidateKey.Equals(emptyIdSentinel))
                {
                    return false;
                }
                return candidateKey.Equals(existingKey);
            });

            if (updatedEntity == null)
            {
                _context.Entry(existingEntity).State = EntityState.Deleted;
                existingCollection.Remove(existingEntity);
            }
            else
            {
                updatedEntity.Id = existingEntity.Id;
                var entry = _context.Entry(existingEntity);
                entry.CurrentValues.SetValues(updatedEntity);
                entry.State = EntityState.Modified;
            }
        }

        foreach (var newEntity in updatedList)
        {
            var key = (object?)keySelector(newEntity);
            var matched = existingList.Any(ex =>
            {
                var existingKey = (object?)keySelector(ex);
                if (existingKey is null || key is null)
                {
                    return false;
                }
                if (key.Equals(emptyIdSentinel))
                {
                    return false;
                }
                return existingKey.Equals(key);
            });
            if (matched)
            {
                continue;
            }

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
                updatedEntity.Id = existingEntity.Id;
                var entry = _context.Entry(existingEntity);
                entry.CurrentValues.SetValues(updatedEntity);
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
