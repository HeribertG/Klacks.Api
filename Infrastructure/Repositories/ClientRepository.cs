using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly DataBaseContext context;
    private readonly IMacroEngine macroEngine;
    private readonly IClientChangeTrackingService _changeTrackingService;
    private readonly IClientEntityManagementService _entityManagementService;
    private readonly EntityCollectionUpdateService _collectionUpdateService;

    public ClientRepository(
        DataBaseContext context,
        IMacroEngine macroEngine,
        IClientChangeTrackingService changeTrackingService,
        IClientEntityManagementService entityManagementService,
        EntityCollectionUpdateService collectionUpdateService)
    {
        this.context = context;
        this.macroEngine = macroEngine;
        _changeTrackingService = changeTrackingService;
        _entityManagementService = entityManagementService;
        _collectionUpdateService = collectionUpdateService;
    }

    public async Task Add(Client client)
    {
        _entityManagementService.PrepareClientForAdd(client);
        this.context.Client.Add(client);
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
        var client = await this.context.Client.Include(a => a.Addresses)
                                             .Include(co => co.Communications)
                                             .Include(co => co.Membership)
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
        var res = await this.context.Client.Include(cu => cu.Addresses)
                                      .Include(cu => cu.Communications)
                                      .Include(cu => cu.Annotations)
                                      .Include(cu => cu.Membership)
                                      .Include(cu => cu.Breaks)
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
        return await this.context.Client.Include(cu => cu.Addresses)
                                        .Include(cu => cu.Communications)
                                        .Include(cu => cu.Annotations)
                                        .Include(cu => cu.Membership)
                                        .Include(cu => cu.Breaks)
                                        .ToListAsync();
    }

    public async Task<Client> Put(Client client)
    {
        var existingClient = await this.context.Client
            .Include(c => c.Addresses)
            .Include(c => c.Communications)
            .Include(c => c.Annotations)
            .Include(c => c.Membership)
            .Include(c => c.Breaks)
            .FirstOrDefaultAsync(c => c.Id == client.Id);

        if (existingClient == null)
        {
            throw new KeyNotFoundException($"Client with ID {client.Id} not found");
        }

        var entry = this.context.Entry(existingClient);
        entry.CurrentValues.SetValues(client);
        entry.State = EntityState.Modified;

        UpdateNestedEntitiesManually(existingClient, client);

        return existingClient;
    }

    private void UpdateNestedEntitiesManually(Client existingClient, Client updatedClient)
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

        _collectionUpdateService.UpdateSingleEntity(
            existingClient.Membership,
            updatedClient.Membership,
            existingClient.Id,
            (membership, clientId) => membership.ClientId = clientId,
            (membership) => existingClient.Membership = membership,
            () => existingClient.Membership = null);
    }

    public void Remove(Client client)
    {
        this.context.Client.Remove(client);
    }
}
