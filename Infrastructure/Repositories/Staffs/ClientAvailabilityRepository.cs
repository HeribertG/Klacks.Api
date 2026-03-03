// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientAvailabilityRepository : BaseRepository<ClientAvailability>, IClientAvailabilityRepository
{
    private readonly DataBaseContext _context;

    public ClientAvailabilityRepository(
        DataBaseContext context,
        ILogger<ClientAvailability> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<List<ClientAvailability>> GetByDateRange(DateOnly start, DateOnly end)
    {
        return await _context.ClientAvailability
            .Where(ca => ca.Date >= start && ca.Date <= end)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<ClientAvailability>> GetByClientAndDateRange(Guid clientId, DateOnly start, DateOnly end)
    {
        return await _context.ClientAvailability
            .Where(ca => ca.ClientId == clientId && ca.Date >= start && ca.Date <= end)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task BulkUpsert(List<ClientAvailability> items)
    {
        foreach (var item in items)
        {
            var existing = await _context.ClientAvailability
                .FirstOrDefaultAsync(ca =>
                    ca.ClientId == item.ClientId &&
                    ca.Date == item.Date &&
                    ca.Hour == item.Hour);

            if (existing != null)
            {
                existing.IsAvailable = item.IsAvailable;
            }
            else
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                await _context.ClientAvailability.AddAsync(item);
            }
        }
    }
}
