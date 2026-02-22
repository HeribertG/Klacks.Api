// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientEntityManagementService : IClientEntityManagementService
{
    private readonly IClientValidator _clientValidator;

    public ClientEntityManagementService(IClientValidator clientValidator)
    {
        _clientValidator = clientValidator;
    }
    public void UpdateNestedEntities<TEntity>(
        Guid clientId,
        Guid[] existingEntityIds,
        Func<Guid, IQueryable<TEntity>> fetchEntities,
        Action<TEntity> removeEntity) where TEntity : class
    {
        IEnumerable<TEntity> entitiesToRemove;

        if (existingEntityIds.Length == 0)
        {
            entitiesToRemove = fetchEntities(clientId).AsEnumerable();
        }
        else
        {
            entitiesToRemove = fetchEntities(clientId)
                .AsEnumerable()
                .Where(e => !existingEntityIds.Contains(
                    (Guid)e.GetType().GetProperty("Id")!.GetValue(e)!));
        }

        foreach (var entity in entitiesToRemove)
        {
            removeEntity(entity);
        }
    }

    public void PrepareClientForAdd(Client client)
    {
        _clientValidator.RemoveEmptyCollections(client);
        client.IdNumber = 0;
    }
}