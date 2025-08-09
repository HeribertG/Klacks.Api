using Klacks.Api.Datas;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Infrastructure.Repositories;

public class AddressRepository : BaseRepository<Address>, IAddressRepository
{
    private readonly DataBaseContext context;

    public AddressRepository(DataBaseContext context, ILogger<Address> logger)
      : base(context, logger)
    {
        this.context = context;
    }

    public async Task<List<Address>> AddressList(Guid id)
    {
        return await this.context.Address.IgnoreQueryFilters().Where(x => x.ClientId == id).OrderByDescending(x => x.ValidFrom).ToListAsync();
    }

    public async Task<List<Address>> SimpleList(Guid id)
    {
        Logger.LogInformation("Fetching simple address list for ID: {ClientId}", id);
        var addresses = await this.context.Address.Where(c => c.ClientId == id).ToListAsync();
        if (!addresses.Any())
        {
            Logger.LogWarning("SimpleList: No addresses found for client ID: {ClientId}.", id);
            throw new ValidationException($"No addresses found for client ID: {id}.");
        }

        Logger.LogInformation("SimpleList: Retrieved {Count} addresses for client ID: {ClientId}.", addresses.Count, id);
        return addresses;
    }

    public async Task<List<Address>> ClienList(Guid id)
    {
        Logger.LogInformation("Fetching client list for ID: {ClientId}", id);
        var addresses = await this.context.Address.Where(c => c.ClientId == id).ToListAsync();
        if (!addresses.Any())
        {
            Logger.LogWarning("ClientList: No addresses found for client ID: {ClientId}.", id);
            throw new ValidationException($"No addresses found for client ID: {id}.");
        }

        Logger.LogInformation("ClientList: Retrieved {Count} addresses for client ID: {ClientId}.", addresses.Count, id);
        return addresses;
    }
}
