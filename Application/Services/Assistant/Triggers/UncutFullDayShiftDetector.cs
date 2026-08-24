// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans ShiftType.IsTask rows in Status OriginalShift whose StartShift equals EndShift -- the
/// FullDay convention TimeRange.ForWorkingTime assigns to equal bounds, a 24 hour duty rather than
/// a zero-length span -- and that have not yet been cut into day/night pieces (a cut mutates the
/// row's Status to SplitShift, see CutShiftByDateSkill). Container shifts (ShiftType.IsContainer)
/// are excluded on purpose: "cut" is a task-level operation, containers are pure envelopes, so a
/// container with equal StartShift/EndShift is exclusively EmptyContainerDetector's concern, never
/// this detector's. Already-ended shifts (UntilDate before today) are excluded so old, resolved
/// duties never resurface; a shift already under way (FromDate in the past, UntilDate still open)
/// is kept and, being closest to today, ranks as the most urgent finding rather than being dropped
/// as stale. Ranks candidates by proximity to today (nearest FromDate first, in either time
/// direction) before capping emission at MaxFindingsPerTick, so a large backlog of long-since-
/// started duties cannot crowd out genuinely upcoming ones.
/// </summary>
/// <param name="shiftRepository">Read-only shift scans via GetQuery().</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UncutFullDayShiftDetector : IAgentTriggerDetector
{
    public const int MaxFindingsPerTick = 25;
    private const int MaxCandidatesToScan = 500;

    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<UncutFullDayShiftDetector> _logger;

    public UncutFullDayShiftDetector(
        IShiftRepository shiftRepository,
        ILogger<UncutFullDayShiftDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UncutFulldayShift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var candidates = await _shiftRepository.GetQuery()
            .Where(s => s.Status == ShiftStatus.OriginalShift
                && s.ShiftType == ShiftType.IsTask
                && s.StartShift == s.EndShift
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null
                && !s.IsDeleted
                && (s.UntilDate == null || s.UntilDate >= today))
            .Take(MaxCandidatesToScan)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = candidates
            .Select(shift => new
            {
                Shift = shift,
                DaysUntil = shift.FromDate.DayNumber - today.DayNumber
            })
            .OrderBy(x => Math.Abs(x.DaysUntil))
            .Take(MaxFindingsPerTick)
            .Select(x => (IAgentTriggerEvent)new UncutFullDayShiftTriggerEvent(
                x.Shift.Id,
                x.Shift.FromDate,
                x.DaysUntil,
                null))
            .ToList();

        _logger.LogInformation(
            "UncutFullDayShift scan: {Total} uncut full-day shift(s) found, {Events} event(s) emitted",
            candidates.Count, events.Count);

        return events;
    }
}
