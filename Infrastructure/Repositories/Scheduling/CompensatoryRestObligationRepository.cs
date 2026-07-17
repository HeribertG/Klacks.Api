// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ICompensatoryRestObligationRepository"/>.
/// </summary>
/// <param name="context">Database context providing the CompensatoryRestObligation DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class CompensatoryRestObligationRepository : ICompensatoryRestObligationRepository
{
    private readonly DataBaseContext _context;

    public CompensatoryRestObligationRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<CompensatoryRestObligation>> GetActiveByClientInWindowAsync(
        Guid clientId,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        return await _context.CompensatoryRestObligation
            .Where(o => !o.IsDeleted
                && o.ClientId == clientId
                && o.TriggerDate <= windowEnd
                && o.DueDate >= windowStart)
            .ToListAsync();
    }

    public async Task<List<CompensatoryRestObligation>> GetOpenByClientAsync(Guid clientId)
    {
        return await _context.CompensatoryRestObligation
            .Where(o => !o.IsDeleted && o.ClientId == clientId && o.FulfilledOn == null)
            .OrderBy(o => o.TriggerDate)
            .ToListAsync();
    }

    public void Add(CompensatoryRestObligation obligation)
    {
        _context.CompensatoryRestObligation.Add(obligation);
    }

    public void Update(CompensatoryRestObligation obligation)
    {
        _context.CompensatoryRestObligation.Update(obligation);
    }

    public void Delete(CompensatoryRestObligation obligation)
    {
        _context.Remove(obligation);
    }
}
