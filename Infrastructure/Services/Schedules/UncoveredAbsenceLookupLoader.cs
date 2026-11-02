// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the <see cref="UncoveredAbsenceLookup"/> of a check from rows it already loaded, reading only the shift and
/// absence names on top. Soft-deleted shifts and absence types are included, so a retired one keeps its name on the
/// rows that still point to it. Works without a break of their client cost no name lookup.
/// </summary>
/// <param name="context">Database context of the caller</param>
/// <param name="works">Works of the check; all carry the check's scenario token</param>
/// <param name="workChanges">Live changes of those works</param>
/// <param name="breaks">Top-level breaks of the check</param>
/// <param name="onCallBreakIds">Ids of the breaks that stand for an on-call absence</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class UncoveredAbsenceLookupLoader
{
    public static async Task<UncoveredAbsenceLookup> LoadAsync(
        DataBaseContext context,
        IReadOnlyCollection<Work> works,
        IReadOnlyCollection<WorkChange> workChanges,
        IReadOnlyCollection<Break> breaks,
        IReadOnlySet<Guid> onCallBreakIds,
        CancellationToken cancellationToken)
    {
        var absenceBreaks = breaks.Where(b => !onCallBreakIds.Contains(b.Id)).ToList();
        var absentClientIds = absenceBreaks.Select(b => b.ClientId).ToHashSet();
        var worksOfAbsentClients = works.Where(w => absentClientIds.Contains(w.ClientId)).ToList();
        if (worksOfAbsentClients.Count == 0)
        {
            return new UncoveredAbsenceLookup(
                onCallBreakIds,
                new Dictionary<Guid, IReadOnlyList<ReplacementInterval>>(),
                new Dictionary<Guid, string>(),
                new Dictionary<Guid, MultiLanguage>(),
                new Dictionary<Guid, UncoveredAbsenceWork>());
        }

        var shifts = await ShiftLabels(context, worksOfAbsentClients.Select(w => w.ShiftId).Distinct().ToList())
            .ToListAsync(cancellationToken);
        var absences = await AbsenceNames(context, absenceBreaks.Select(b => b.AbsenceId).Distinct().ToList())
            .ToListAsync(cancellationToken);
        var absenceNames = absences.ToDictionary(absence => absence.Id, absence => absence.Name);

        return new UncoveredAbsenceLookup(
            onCallBreakIds,
            BuildReplacementWindows(worksOfAbsentClients, workChanges),
            shifts.ToDictionary(
                shift => shift.Id,
                shift => string.IsNullOrEmpty(shift.Abbreviation) ? shift.Name : shift.Abbreviation),
            absenceBreaks
                .Where(b => absenceNames.ContainsKey(b.AbsenceId))
                .ToDictionary(b => b.Id, b => absenceNames[b.AbsenceId]),
            worksOfAbsentClients.ToDictionary(work => work.Id, ToUncoveredAbsenceWork));
    }

    internal static IQueryable<Shift> ShiftLabels(DataBaseContext context, IReadOnlyCollection<Guid> shiftIds)
        => context.Shift
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(shift => shiftIds.Contains(shift.Id))
            .Select(shift => new Shift { Id = shift.Id, Abbreviation = shift.Abbreviation, Name = shift.Name });

    internal static IQueryable<Absence> AbsenceNames(DataBaseContext context, IReadOnlyCollection<Guid> absenceIds)
        => context.Absence
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(absence => absenceIds.Contains(absence.Id));

    private static Dictionary<Guid, IReadOnlyList<ReplacementInterval>> BuildReplacementWindows(
        IReadOnlyCollection<Work> works, IReadOnlyCollection<WorkChange> workChanges)
    {
        var worksById = works.ToDictionary(work => work.Id);

        return workChanges
            .Where(change => change.ReplaceClientId.HasValue
                && ReplacementWorkChangeTypes.IsReplacement(change.Type)
                && worksById.ContainsKey(change.WorkId))
            .GroupBy(change => change.WorkId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ReplacementInterval>)group
                    .Select(change => ToInterval(worksById[group.Key], change))
                    .OfType<ReplacementInterval>()
                    .ToList());
    }

    private static UncoveredAbsenceWork ToUncoveredAbsenceWork(Work work)
    {
        var (startAt, endAt) = ReplacementWindow.ToInterval(work.CurrentDate, work.StartTime, work.StartTime, work.EndTime);
        return new UncoveredAbsenceWork(work.ShiftId, work.CurrentDate, new ReplacementInterval(startAt, endAt));
    }

    private static ReplacementInterval? ToInterval(Work work, WorkChange change)
        => ReplacementWindow.TryGetInterval(
            change.Type, work.StartTime, work.EndTime, work.CurrentDate, change.StartTime, change.EndTime, change.ChangeTime,
            out var startAt, out var endAt)
            ? new ReplacementInterval(startAt, endAt)
            : null;
}
