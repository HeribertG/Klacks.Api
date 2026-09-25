// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IMacroAssignmentHistoryRepository"/> over the macro_assignment_history table. Stage-only; the
/// soft-delete query filter hides deleted rows; the rows of one switch are returned tracked in creation order; the latest
/// row of a holder is the one with the newest creation time, and on equal creation times the one with the higher id
/// (a deterministic tie-breaker, not a chronological one).
/// </summary>
/// <param name="context">Database context providing the MacroAssignmentHistory DbSet</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class MacroAssignmentHistoryRepository : IMacroAssignmentHistoryRepository
{
    private readonly DataBaseContext _context;

    public MacroAssignmentHistoryRepository(DataBaseContext context)
    {
        _context = context;
    }

    public void Add(MacroAssignmentHistory entry)
    {
        _context.MacroAssignmentHistory.Add(entry);
    }

    public async Task<IReadOnlyList<MacroAssignmentHistory>> GetSwitchAsync(
        Guid switchId, CancellationToken cancellationToken = default)
    {
        return await _context.MacroAssignmentHistory
            .Where(h => h.SwitchId == switchId)
            .OrderBy(h => h.CreateTime)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<MacroAssignmentHistory?> GetLatestAsync(
        MacroAssignmentTarget target, Guid targetId, CancellationToken cancellationToken = default)
    {
        return await _context.MacroAssignmentHistory
            .AsNoTracking()
            .Where(h => h.Target == target && h.TargetId == targetId)
            .OrderByDescending(h => h.CreateTime)
            .ThenByDescending(h => h.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
