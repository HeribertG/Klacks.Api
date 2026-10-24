// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class AddressRepository : BaseRepository<Address>, IAddressRepository
{
    public AddressRepository(DataBaseContext context, ILogger<Address> logger)
      : base(context, logger)
    {
    }

    public async Task<List<Address>> AddressList(Guid id)
    {
        return await this.context.Address.IgnoreQueryFilters().Where(x => x.ClientId == id).OrderByDescending(x => x.ValidFrom).ToListAsync();
    }

    public async Task<List<Address>> SimpleList(Guid id)
    {
        Logger.LogInformation("Fetching simple address list for ID: {ClientId}", id);
        var addresses = await this.context.Address.Where(c => c.ClientId == id).ToListAsync();
        Logger.LogInformation("SimpleList: Retrieved {Count} addresses for client ID: {ClientId}.", addresses.Count, id);
        return addresses;
    }

    public async Task<List<Address>> ClienList(Guid id)
    {
        Logger.LogInformation("Fetching client list for ID: {ClientId}", id);
        var addresses = await this.context.Address.Where(c => c.ClientId == id).ToListAsync();
        Logger.LogInformation("ClientList: Retrieved {Count} addresses for client ID: {ClientId}.", addresses.Count, id);
        return addresses;
    }

    public async Task<List<CityCentroid>> GetCityCentroidsAsync(CancellationToken cancellationToken = default)
    {
        return await this.context.Address
            .AsNoTracking()
            .Where(a => a.City != string.Empty && a.Latitude != null && a.Longitude != null)
            .GroupBy(a => a.City.Trim().ToLower())
            .Select(g => new CityCentroid(
                g.Key,
                g.Average(a => a.Latitude!.Value),
                g.Average(a => a.Longitude!.Value),
                g.Count()))
            .ToListAsync(cancellationToken);
    }
}
