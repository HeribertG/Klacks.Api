// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Staffs;
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
        if (items.Count == 0)
        {
            return;
        }

        var storedBySlot = await LoadStoredRowsIncludingDeleted(
            items.Select(i => new ClientAvailabilitySlot(i.ClientId, i.Date, i.Hour)).ToList());

        foreach (var item in items)
        {
            if (storedBySlot.TryGetValue(new ClientAvailabilitySlot(item.ClientId, item.Date, item.Hour), out var existing))
            {
                if (existing.IsDeleted)
                {
                    Restore(existing);
                }

                existing.IsAvailable = item.IsAvailable;
                continue;
            }

            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            await _context.ClientAvailability.AddAsync(item);
        }
    }

    public async Task BulkRemove(IReadOnlyCollection<ClientAvailabilitySlot> slots)
    {
        if (slots.Count == 0)
        {
            return;
        }

        var storedBySlot = await LoadStoredRowsIncludingDeleted(slots);

        foreach (var slot in slots)
        {
            if (storedBySlot.TryGetValue(slot, out var existing) && !existing.IsDeleted)
            {
                _context.ClientAvailability.Remove(existing);
            }
        }
    }

    private async Task<Dictionary<ClientAvailabilitySlot, ClientAvailability>> LoadStoredRowsIncludingDeleted(
        IReadOnlyCollection<ClientAvailabilitySlot> slots)
    {
        var clientIds = slots.Select(s => s.ClientId).Distinct().ToList();
        var firstDate = slots.Min(s => s.Date);
        var lastDate = slots.Max(s => s.Date);
        var requested = slots.ToHashSet();

        var candidates = await _context.ClientAvailability
            .IgnoreQueryFilters()
            .Where(ca => clientIds.Contains(ca.ClientId) && ca.Date >= firstDate && ca.Date <= lastDate)
            .ToListAsync();

        return candidates
            .Select(ca => (Slot: new ClientAvailabilitySlot(ca.ClientId, ca.Date, ca.Hour), Row: ca))
            .Where(x => requested.Contains(x.Slot))
            .GroupBy(x => x.Slot)
            .ToDictionary(g => g.Key, g => g.First().Row);
    }

    private static void Restore(ClientAvailability row)
    {
        row.IsDeleted = false;
        row.DeletedTime = null;
        row.CurrentUserDeleted = null;
    }

    public async Task<List<ClientAvailabilityTotalResource>> GetTotalsByClientsAndDateRange(
        List<Guid> clientIds, DateOnly start, DateOnly end)
    {
        var dailyHourCounts = await _context.ClientAvailability
            .Where(ca => clientIds.Contains(ca.ClientId)
                         && ca.Date >= start
                         && ca.Date <= end
                         && ca.IsAvailable)
            .GroupBy(ca => new { ca.ClientId, ca.Date })
            .Select(g => new { g.Key.ClientId, g.Key.Date, HourCount = g.Count() })
            .ToListAsync();

        return dailyHourCounts
            .GroupBy(x => x.ClientId)
            .Select(g => new ClientAvailabilityTotalResource
            {
                ClientId = g.Key,
                TotalHours = g.Sum(x => x.HourCount),
                DaysWithAvailability = g.Count()
            })
            .ToList();
    }
}
