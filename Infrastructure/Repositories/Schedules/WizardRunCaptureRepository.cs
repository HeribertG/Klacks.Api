// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core implementation of <see cref="IWizardRunCaptureRepository"/>. Persists a capture together
/// with its created-work child links, resolves captures by scenario id, and serves the deferred
/// post-apply measurement: it loads the proposal works with <c>IgnoreQueryFilters</c> so soft-deleted
/// rows (a correction signal) stay visible, flags proposal works carrying a post-apply non-recovery
/// WorkChange overlay (the other correction signal), and gathers post-apply Break and recovery-marker
/// event cells. A group-scoped seal additionally recovers group-less direct-apply captures whose created
/// works belong to clients of the sealed group. Writes are self-contained (own SaveChanges) so a capture
/// failure is fully isolated from the surrounding apply transaction.
/// </summary>
/// <param name="context">EF Core database context</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public sealed class WizardRunCaptureRepository : IWizardRunCaptureRepository
{
    private readonly DataBaseContext _context;

    public WizardRunCaptureRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WizardRunCapture capture, IReadOnlyList<Guid> workIds, CancellationToken ct = default)
    {
        capture.Works = workIds
            .Select(id => new WizardRunCaptureWork { CaptureId = capture.Id, WorkId = id })
            .ToList();

        await _context.WizardRunCapture.AddAsync(capture, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<WizardRunCapture?> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct = default)
    {
        return await _context.WizardRunCapture
            .Where(c => c.ScenarioId == scenarioId && !c.IsDeleted)
            .OrderByDescending(c => c.CreateTime)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SetOutcomeAsync(Guid captureId, CaptureOutcome outcome, CancellationToken ct = default)
    {
        var capture = await _context.WizardRunCapture
            .FirstOrDefaultAsync(c => c.Id == captureId && !c.IsDeleted, ct);

        if (capture is null)
        {
            return;
        }

        capture.Outcome = outcome;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<CaptureScenarioState?> GetScenarioStateAsync(Guid scenarioId, CancellationToken ct = default)
    {
        return await _context.AnalyseScenarios
            .IgnoreQueryFilters()
            .Where(s => s.Id == scenarioId)
            .Select(s => new CaptureScenarioState(s.Status, s.IsDeleted))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<WizardRunCapture>> GetAllForReportAsync(
        DateOnly? from, DateOnly? until, Guid? groupId, CancellationToken ct = default)
    {
        var query = _context.WizardRunCapture
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (until.HasValue)
        {
            query = query.Where(c => c.PeriodFrom <= until.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(c => c.PeriodUntil >= from.Value);
        }

        if (groupId.HasValue)
        {
            query = query.Where(c => c.GroupId == groupId.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<int> SupersedeOpenDirectCapturesAsync(
        Guid newCaptureId,
        WizardEngine engine,
        DateOnly periodFrom,
        DateOnly periodUntil,
        CancellationToken ct = default)
    {
        var open = await _context.WizardRunCapture
            .Where(c => !c.IsDeleted
                        && c.MeasuredAt == null
                        && c.Outcome == null
                        && c.ApplyKind == WizardApplyKind.Direct
                        && c.Engine == engine
                        && c.Id != newCaptureId
                        && c.PeriodFrom <= periodUntil
                        && c.PeriodUntil >= periodFrom)
            .ToListAsync(ct);

        if (open.Count == 0)
        {
            return 0;
        }

        foreach (var capture in open)
        {
            capture.Outcome = CaptureOutcome.Superseded;
        }

        await _context.SaveChangesAsync(ct);
        return open.Count;
    }

    public async Task<IReadOnlyList<WizardRunCapture>> GetUnmeasuredForSealAsync(
        DateOnly periodFrom, DateOnly periodUntil, Guid? groupId, CancellationToken ct = default)
    {
        var direct = await _context.WizardRunCapture
            .Where(c => !c.IsDeleted
                        && c.MeasuredAt == null
                        && c.Outcome == null
                        && c.PeriodFrom <= periodUntil
                        && c.PeriodUntil >= periodFrom
                        && (groupId == null || c.GroupId == groupId))
            .ToListAsync(ct);

        // Global seal already matches every group; nothing more to add.
        if (groupId == null)
        {
            return direct;
        }

        // Direct-apply captures carry GroupId=null, so a group-scoped seal misses them above. Recover the ones
        // whose created works belong to clients that are members of the sealed group over the seal period, so a
        // group seal still measures the direct-apply plans it finalised instead of letting them expire later.
        var periodFromDt = periodFrom.ToDateTime(TimeOnly.MinValue);
        var periodUntilDt = periodUntil.ToDateTime(TimeOnly.MaxValue);

        var groupClientIds = await _context.GroupItem
            .Where(gi => gi.GroupId == groupId
                         && gi.ClientId != null
                         && (gi.ValidFrom == null || gi.ValidFrom <= periodUntilDt)
                         && (gi.ValidUntil == null || gi.ValidUntil >= periodFromDt))
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var qualifyingCaptureIds = await _context.WizardRunCaptureWork
            .Where(link => !link.IsDeleted)
            .Join(
                _context.Work.IgnoreQueryFilters(),
                link => link.WorkId,
                work => work.Id,
                (link, work) => new { link.CaptureId, work.ClientId })
            .Where(x => groupClientIds.Contains(x.ClientId))
            .Select(x => x.CaptureId)
            .Distinct()
            .ToListAsync(ct);

        var extra = await _context.WizardRunCapture
            .Where(c => !c.IsDeleted
                        && c.MeasuredAt == null
                        && c.Outcome == null
                        && c.PeriodFrom <= periodUntil
                        && c.PeriodUntil >= periodFrom
                        && c.GroupId == null
                        && qualifyingCaptureIds.Contains(c.Id))
            .ToListAsync(ct);

        // direct (c.GroupId == groupId) and extra (c.GroupId == null) are disjoint by construction.
        return direct.Concat(extra).ToList();
    }

    public async Task<IReadOnlyList<WizardRunCapture>> GetUnmeasuredExpiredAsync(
        DateOnly periodEndedBefore, CancellationToken ct = default)
    {
        return await _context.WizardRunCapture
            .Where(c => !c.IsDeleted
                        && c.MeasuredAt == null
                        && c.Outcome == null
                        && c.PeriodUntil < periodEndedBefore)
            .ToListAsync(ct);
    }

    public async Task<WizardRunMeasurementData> LoadMeasurementDataAsync(
        WizardRunCapture capture, string recoveryMarker, CancellationToken ct = default)
    {
        var anchor = capture.CreateTime ?? DateTime.MinValue;

        var workIds = await _context.WizardRunCaptureWork
            .Where(l => l.CaptureId == capture.Id && !l.IsDeleted)
            .Select(l => l.WorkId)
            .ToListAsync(ct);

        var works = await _context.Work
            .IgnoreQueryFilters()
            .Where(w => workIds.Contains(w.Id))
            .Select(w => new
            {
                w.Id,
                w.ClientId,
                w.CurrentDate,
                w.ShiftId,
                w.StartTime,
                w.EndTime,
                w.IsDeleted,
            })
            .ToListAsync(ct);

        var overlaidWorkIds = (await _context.WorkChange
            .Where(wc => workIds.Contains(wc.WorkId)
                         && wc.CreateTime > anchor
                         && wc.Description != recoveryMarker)
            .Select(wc => wc.WorkId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();

        var proposalCells = works
            .Select(w => new WizardRunProposalCell(
                w.ClientId,
                w.CurrentDate,
                w.ShiftId,
                w.StartTime,
                w.EndTime,
                w.IsDeleted,
                overlaidWorkIds.Contains(w.Id)))
            .ToList();

        var eventCells = new HashSet<(Guid AgentId, DateOnly Date)>();

        var breakCells = await _context.Break
            .Where(b => !b.IsDeleted
                        && b.CreateTime > anchor
                        && b.CurrentDate >= capture.PeriodFrom
                        && b.CurrentDate <= capture.PeriodUntil)
            .Select(b => new { b.ClientId, b.CurrentDate })
            .ToListAsync(ct);

        foreach (var b in breakCells)
        {
            eventCells.Add((b.ClientId, b.CurrentDate));
        }

        var recoveryChanges = await (
            from wc in _context.WorkChange
            join w in _context.Work.IgnoreQueryFilters() on wc.WorkId equals w.Id
            where wc.Description == recoveryMarker
                  && wc.CreateTime > anchor
                  && w.CurrentDate >= capture.PeriodFrom
                  && w.CurrentDate <= capture.PeriodUntil
            select new { w.ClientId, w.CurrentDate, wc.ReplaceClientId })
            .ToListAsync(ct);

        foreach (var rc in recoveryChanges)
        {
            eventCells.Add((rc.ClientId, rc.CurrentDate));
            if (rc.ReplaceClientId.HasValue)
            {
                eventCells.Add((rc.ReplaceClientId.Value, rc.CurrentDate));
            }
        }

        return new WizardRunMeasurementData(proposalCells, eventCells);
    }

    public async Task SetMeasurementAsync(
        Guid captureId,
        double correctionChurn,
        double eventChurn,
        DateTime measuredAt,
        CaptureOutcome outcome,
        CancellationToken ct = default)
    {
        var capture = await _context.WizardRunCapture
            .FirstOrDefaultAsync(c => c.Id == captureId && !c.IsDeleted, ct);

        if (capture is null || capture.Outcome != null || capture.MeasuredAt != null)
        {
            return;
        }

        capture.CorrectionChurn = correctionChurn;
        capture.EventChurn = eventChurn;
        capture.MeasuredAt = measuredAt;
        capture.Outcome = outcome;
        await _context.SaveChangesAsync(ct);
    }
}
