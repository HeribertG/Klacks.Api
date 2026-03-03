// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IClientEntityManagementService
{
    void UpdateNestedEntities<TEntity>(Guid clientId, Guid[] existingEntityIds,
        Func<Guid, IQueryable<TEntity>> fetchEntities, Action<TEntity> removeEntity) where TEntity : class;
    void PrepareClientForAdd(Client client);
}