using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly DataBaseContext context;
    private readonly IMacroEngine macroEngine;
    private readonly IClientChangeTrackingService _changeTrackingService;
    private readonly IClientEntityManagementService _entityManagementService;
    private readonly EntityCollectionUpdateService _collectionUpdateService;
    private readonly IClientValidator _clientValidator;
    private readonly ILogger<ClientRepository> _logger;

    public ClientRepository(
        DataBaseContext context,
        IMacroEngine macroEngine,
        IClientChangeTrackingService changeTrackingService,
        IClientEntityManagementService entityManagementService,
        EntityCollectionUpdateService collectionUpdateService,
        IClientValidator clientValidator,
        ILogger<ClientRepository> logger)
    {
        this.context = context;
        this.macroEngine = macroEngine;
        _logger = logger;
        _changeTrackingService = changeTrackingService;
        _entityManagementService = entityManagementService;
        _collectionUpdateService = collectionUpdateService;
        _clientValidator = clientValidator;
    }

    public async Task Add(Client client)
    {
        _entityManagementService.PrepareClientForAdd(client);
        _clientValidator.EnsureUniqueGroupItems(client.GroupItems);
        _clientValidator.EnsureUniqueClientContracts(client.ClientContracts);
        _clientValidator.EnsureSingleActiveContract(client.ClientContracts);

        if (client.ClientImage != null)
        {
            client.ClientImage.ClientId = client.Id;
            client.ClientImage.CreateTime = DateTime.UtcNow;
        }

        await this.context.Client.AddAsync(client);
    }

    public int Count()
    {
        if (this.context.Client.Count() > 0)
        {
            return this.context.Client.IgnoreQueryFilters().Max(c => c.IdNumber);
        }
        else
        {
            return 0;
        }
    }

    public async Task<Client?> Delete(Guid id)
    {
        var client = await this.context.Client
                                             .Include(co => co.Membership)
                                             .Include(a => a.Addresses)
                                             .Include(co => co.Communications)
                                             .AsSplitQuery()
                                             .SingleOrDefaultAsync(emp => emp.Id == id);

        if (client != null)
        {
            this.context.Client.Remove(client);
        }

        return client;
    }

    public async Task<bool> Exists(Guid id)
    {
        return await this.context.Client.AnyAsync(e => e.Id == id);
    }

    public async Task<Client?> Get(Guid id)
    {
        var res = await this.context.Client
                                      .Include(cu => cu.Membership)
                                      .Include(cu => cu.Addresses)
                                      .Include(cu => cu.Communications)
                                      .Include(cu => cu.Annotations)
                                      .Include(cu => cu.BreakPlaceholders)
                                      .Include(cu => cu.ClientContracts)
                                          .ThenInclude(cc => cc.Contract)
                                      .Include(cu => cu.GroupItems)
                                          .ThenInclude(gi => gi.Group)
                                      .Include(cu => cu.ClientImage)
                                      .AsSplitQuery()
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(emp => emp.Id == id);

        return res!;
    }

    public async Task<LastChangeMetaData> LastChangeMetaData()
    {
        var result = await _changeTrackingService.GetLastChangeMetadataAsync();
        return new LastChangeMetaData
        {
            LastChangesDate = result.lastChangeDate,
            Author = string.Join(", ", result.authors)
        };
    }

    public async Task<List<Client>> List()
    {
        return await this.context.Client
                                        .Include(cu => cu.Membership)
                                        .Include(cu => cu.Addresses)
                                        .Include(cu => cu.Communications)
                                        .Include(cu => cu.Annotations)
                                        .Include(cu => cu.BreakPlaceholders)
                                        .AsSplitQuery()
                                        .ToListAsync();
    }

    public async Task<Client?> Put(Client client)
    {
        var existingClient = await this.context.Client
            .Include(c => c.Membership)
            .Include(c => c.Addresses)
            .Include(c => c.Communications)
            .Include(c => c.Annotations)
            .Include(c => c.BreakPlaceholders)
            .Include(c => c.ClientContracts)
                .ThenInclude(cc => cc.Contract)
            .Include(c => c.GroupItems)
                .ThenInclude(gi => gi.Group)
            .Include(c => c.ClientImage)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == client.Id);

        if (existingClient == null)
        {
            throw new KeyNotFoundException($"Client with ID {client.Id} not found");
        }

        var existingClientImage = existingClient.ClientImage;
        var existingMembership = existingClient.Membership;

        var tempClientImage = client.ClientImage;
        var tempMembership = client.Membership;
        client.ClientImage = null;
        client.Membership = null;

        var entry = this.context.Entry(existingClient);
        entry.CurrentValues.SetValues(client);
        entry.State = EntityState.Modified;

        client.ClientImage = tempClientImage;
        client.Membership = tempMembership;

        _clientValidator.RemoveEmptyCollections(client);

        UpdateNestedEntitiesManually(existingClient, client, existingClientImage);

        return existingClient;
    }

    private void UpdateNestedEntitiesManually(Client existingClient, Client updatedClient, ClientImage? existingClientImage)
    {
        _collectionUpdateService.UpdateCollection(
            existingClient.Addresses,
            updatedClient.Addresses,
            existingClient.Id,
            (address, clientId) => address.ClientId = clientId);

        _collectionUpdateService.UpdateCollection(
            existingClient.Communications,
            updatedClient.Communications,
            existingClient.Id,
            (communication, clientId) => communication.ClientId = clientId);

        _collectionUpdateService.UpdateCollection(
            existingClient.Annotations,
            updatedClient.Annotations,
            existingClient.Id,
            (annotation, clientId) => annotation.ClientId = clientId);

        _collectionUpdateService.UpdateCollection(
            existingClient.ClientContracts,
            updatedClient.ClientContracts,
            existingClient.Id,
            (clientContract, clientId) => clientContract.ClientId = clientId);

        _collectionUpdateService.UpdateCollection(
            existingClient.GroupItems,
            updatedClient.GroupItems,
            existingClient.Id,
            (groupItem, clientId) => groupItem.ClientId = clientId);

        _clientValidator.EnsureUniqueGroupItems(existingClient.GroupItems);
        _clientValidator.EnsureUniqueClientContracts(existingClient.ClientContracts);
        _clientValidator.EnsureSingleActiveContract(existingClient.ClientContracts);

        _collectionUpdateService.UpdateSingleEntity(
            existingClient.Membership,
            updatedClient.Membership,
            existingClient.Id,
            (membership, clientId) => membership.ClientId = clientId,
            (membership) => existingClient.Membership = membership,
            () => existingClient.Membership = null);

        if (updatedClient.ClientImage != null)
        {
            if (existingClientImage != null)
            {
                updatedClient.ClientImage.CreateTime = existingClientImage.CreateTime;
                context.ClientImage.Remove(existingClientImage);
            }
            else
            {
                updatedClient.ClientImage.CreateTime = DateTime.UtcNow;
            }

            updatedClient.ClientImage.ClientId = existingClient.Id;
            updatedClient.ClientImage.UpdateTime = DateTime.UtcNow;
            context.ClientImage.Add(updatedClient.ClientImage);
            existingClient.ClientImage = updatedClient.ClientImage;
        }
        else if (existingClientImage != null)
        {
            context.ClientImage.Remove(existingClientImage);
            existingClient.ClientImage = null;
        }
    }

    public void Remove(Client client)
    {
        this.context.Client.Remove(client);
    }

    public async Task<List<Client>> GetActiveClientsWithAddressesAsync(CancellationToken cancellationToken = default)
    {
        return await context.Client
            .Include(c => c.Addresses)
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
