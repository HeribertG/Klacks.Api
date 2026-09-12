// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientRepository : IBaseRepository<Client>
{
    Task<int> CountAsync();
    Task<LastChangeMetaData> LastChangeMetaData();
    IQueryable<Client> GetQuery();
    Task<List<Client>> GetActiveClientsWithAddressesAsync(CancellationToken cancellationToken = default);
    Task<List<Client>> GetByTypeWithAddressesAndGroupItemsAsync(EntityTypeEnum type, CancellationToken cancellationToken = default);
    Task<List<Client>> GetByTypeWithQualificationsAsync(EntityTypeEnum type, CancellationToken cancellationToken = default);
    Task<List<Client>> GetActiveClientsWithAddressesForGroupsAsync(List<Guid> visibleRootIds, CancellationToken cancellationToken = default);
    Task<List<Client>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Client?> GetByLdapExternalIdAsync(string ldapExternalId);
    Task<Client?> GetWithMembershipAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<Client?> FindReusableCustomerAsync(Client candidate, CancellationToken cancellationToken = default);
    Task<List<Client>> SearchByNameAsync(string nameFragment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the client with all update-relevant relations as a tracked entity, intended to be passed to
    /// <see cref="Put(Client, Client)"/> so update flows need only a single database load.
    /// </summary>
    Task<Client?> GetTrackedForUpdate(Guid id);

    /// <summary>
    /// Applies the update to an already loaded tracked entity (from <see cref="GetTrackedForUpdate"/>)
    /// without reloading it from the database.
    /// </summary>
    Task<Client?> Put(Client client, Client existingClient);

    /// <summary>
    /// Synchronises the notes of one client against the incoming list and touches nothing else on the
    /// client: a note with a known id is updated, a note without one is added, and a stored note absent
    /// from the list is soft-deleted. Stage-only, like every other write on this repository — the caller
    /// commits through IUnitOfWork.
    /// </summary>
    /// <param name="clientId">Client whose notes are written</param>
    /// <param name="annotations">Complete note list as it should be afterwards</param>
    /// <returns>The tracked client, or null when no client carries that id</returns>
    Task<Client?> PutAnnotations(Guid clientId, ICollection<Annotation> annotations);
}
