// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="ICompensatoryRestObligationRepository"/>. The open obligations of one client and of several
/// clients come from the same query, the latter over chunks of IdListQueryLimits.ChunkSize ids; both return tracked rows.
/// </summary>
/// <param name="context">Database context providing the CompensatoryRestObligation DbSet</param>

using Klacks.Api.Domain.Constants;
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
        return await QueryOpenByClients([clientId]).ToListAsync();
    }

    public async Task<Dictionary<Guid, List<CompensatoryRestObligation>>> GetOpenByClientsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, List<CompensatoryRestObligation>>();
        foreach (var chunk in clientIds.Distinct().Chunk(IdListQueryLimits.ChunkSize))
        {
            var rows = await QueryOpenByClients(chunk).ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!result.TryGetValue(row.ClientId, out var open))
                {
                    open = [];
                    result[row.ClientId] = open;
                }

                open.Add(row);
            }
        }

        return result;
    }

    internal IQueryable<CompensatoryRestObligation> QueryOpenByClients(Guid[] clientIds)
        => _context.CompensatoryRestObligation
            .Where(o => !o.IsDeleted && clientIds.Contains(o.ClientId) && o.FulfilledOn == null)
            .OrderBy(o => o.TriggerDate);

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
