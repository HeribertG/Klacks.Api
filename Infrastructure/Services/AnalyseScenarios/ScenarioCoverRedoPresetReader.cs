// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the prefill for covering an absence again after its stale cover was refused: the scenario's absences (breaks
/// without a paused work) that have no live real twin of the same person, day and absence type are the absences the cover
/// added itself. Their days give the range, the person and the type are only set when unique. The scenario range is not
/// used, because a swap chain or a source scenario widens it beyond the absence. A cloned absence whose real original was
/// deleted after the cover also has no twin and is counted in; the rows carry no clone link to tell it apart.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioCoverRedoPresetReader
{
    private readonly DataBaseContext _context;

    public ScenarioCoverRedoPresetReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<CoverRedoPreset> FindCoverRedoPresetAsync(Guid token, CancellationToken ct)
    {
        var scenarioAbsences = await _context.Break.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted && b.ParentWorkId == null)
            .Select(b => new { b.ClientId, b.CurrentDate, b.AbsenceId })
            .ToListAsync(ct);
        if (scenarioAbsences.Count == 0)
        {
            return CoverRedoPreset.Empty;
        }

        var clientIds = scenarioAbsences.Select(a => a.ClientId).Distinct().ToList();
        var fromDate = scenarioAbsences.Min(a => a.CurrentDate);
        var untilDate = scenarioAbsences.Max(a => a.CurrentDate);
        var realKeys = (await _context.Break
                .AsNoTracking()
                .Where(b => b.AnalyseToken == null && !b.IsDeleted && b.ParentWorkId == null
                    && clientIds.Contains(b.ClientId)
                    && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate)
                .Select(b => new { b.ClientId, b.CurrentDate, b.AbsenceId })
                .ToListAsync(ct))
            .Select(b => (b.ClientId, b.CurrentDate, b.AbsenceId))
            .ToHashSet();

        var own = scenarioAbsences
            .Where(a => !realKeys.Contains((a.ClientId, a.CurrentDate, a.AbsenceId)))
            .ToList();
        if (own.Count == 0)
        {
            return CoverRedoPreset.Empty;
        }

        var ownClients = own.Select(a => a.ClientId).Distinct().ToList();
        var ownTypes = own.Select(a => a.AbsenceId).Distinct().ToList();
        var clientId = ownClients.Count == 1 ? ownClients[0] : (Guid?)null;
        var absenceId = clientId.HasValue && ownTypes.Count == 1 ? ownTypes[0] : (Guid?)null;

        return new CoverRedoPreset(clientId, absenceId, own.Min(a => a.CurrentDate), own.Max(a => a.CurrentDate));
    }
}
