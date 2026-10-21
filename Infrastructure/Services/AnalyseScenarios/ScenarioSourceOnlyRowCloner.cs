// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Copies the rows only a source scenario has into a scenario built on it (scenario from scenario): its temporary
/// cross-group memberships and its open replacement requests. Without a source scenario both do nothing.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioSourceOnlyRowCloner
{
    private readonly DataBaseContext _context;

    public ScenarioSourceOnlyRowCloner(DataBaseContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Copies the source scenario's temporary cross-group memberships (borrowed employees) into the new scenario, so a
    /// cover that relies on them survives into the child and is promoted with it on accept.
    /// </summary>
    internal async Task CloneScenarioMemberships(ScenarioCloneSource source, Guid token, CancellationToken ct)
    {
        if (source.Token is not { } sourceToken)
        {
            return;
        }

        var memberships = await _context.Set<GroupItem>()
            .Where(gi => !gi.IsDeleted && gi.AnalyseToken == sourceToken && gi.ShiftId == null && gi.ClientId != null)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var membership in memberships)
        {
            await _context.Set<GroupItem>().AddAsync(new GroupItem
            {
                Id = Guid.NewGuid(),
                ClientId = membership.ClientId,
                GroupId = membership.GroupId,
                ShiftId = null,
                ValidFrom = membership.ValidFrom,
                ValidUntil = membership.ValidUntil,
                AnalyseToken = token
            }, ct);
        }
    }

    /// <summary>
    /// Copies the source scenario's not yet applied replacement requests (request book) into the new scenario, so
    /// accepting the child stamps them applied. ShiftId is already the real root shift; WorkChangeId pointed at the source
    /// scenario's clone and is left for accept to fill. The source's own rows stay untouched.
    /// </summary>
    internal async Task CloneOpenReplacementRequests(ScenarioCloneSource source, Guid token, CancellationToken ct)
    {
        if (source.Token is not { } sourceToken)
        {
            return;
        }

        var requests = await _context.ReplacementRequests
            .Where(r => r.AnalyseToken == sourceToken && r.AppliedAtUtc == null)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var request in requests)
        {
            await _context.ReplacementRequests.AddAsync(new ReplacementRequest
            {
                Id = Guid.NewGuid(),
                AbsentClientId = request.AbsentClientId,
                CandidateClientId = request.CandidateClientId,
                ShiftId = request.ShiftId,
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                GroupId = request.GroupId,
                AbsenceId = request.AbsenceId,
                Source = request.Source,
                Outcome = request.Outcome,
                ReportedAtUtc = request.ReportedAtUtc,
                ShiftStartUtc = request.ShiftStartUtc,
                OutcomeAtUtc = request.OutcomeAtUtc,
                OutcomeByUserId = request.OutcomeByUserId,
                AnalyseToken = token
            }, ct);
        }
    }
}
