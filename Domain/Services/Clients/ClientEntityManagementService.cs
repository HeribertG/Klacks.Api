using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientEntityManagementService : IClientEntityManagementService
{
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
                    (Guid)e.GetType().GetProperty("Id").GetValue(e)));
        }

        foreach (var entity in entitiesToRemove)
        {
            removeEntity(entity);
        }
    }

    public void PrepareClientForAdd(Client client)
    {
        // Remove empty annotations
        for (int i = client.Annotations.Count - 1; i > -1; i--)
        {
            var itm = client.Annotations.ToList()[i];
            if (string.IsNullOrEmpty(itm.Note) || string.IsNullOrWhiteSpace(itm.Note))
            {
                client.Annotations.Remove(itm);
            }
        }

        // Reset IdNumber - it will be set by the database
        client.IdNumber = 0;
    }
}