// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The queries of the macro dry-run, kept in one place so their SQL translation can be pinned against the real provider:
/// the live (query filter) and non-scenario works of a set of shifts and breaks of a set of absence types, their sealed
/// subsets, the most recent open entries as samples, and a macro script by id. Every query is untracked.
/// </summary>
/// <param name="context">Database context the queries run against</param>
/// <param name="holderIds">Ids of the shifts (works) or of the absence types (breaks) in scope</param>
/// <param name="sampleSize">Maximum number of sample entries</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Macros;

internal static class MacroDryRunQueries
{
    internal static IQueryable<Work> WorksOfShifts(DataBaseContext context, IReadOnlyCollection<Guid> holderIds) =>
        context.Work.AsNoTracking().Where(w => holderIds.Contains(w.ShiftId) && w.AnalyseToken == null);

    internal static IQueryable<Work> SealedWorksOfShifts(DataBaseContext context, IReadOnlyCollection<Guid> holderIds) =>
        WorksOfShifts(context, holderIds).Where(w => w.LockLevel != WorkLockLevel.None);

    internal static IQueryable<Work> OpenWorkSamples(
        DataBaseContext context, IReadOnlyCollection<Guid> holderIds, int sampleSize) =>
        WorksOfShifts(context, holderIds)
            .Where(w => w.LockLevel == WorkLockLevel.None)
            .OrderByDescending(w => w.CurrentDate)
            .ThenByDescending(w => w.StartTime)
            .Take(sampleSize);

    internal static IQueryable<Break> BreaksOfAbsenceTypes(DataBaseContext context, IReadOnlyCollection<Guid> holderIds) =>
        context.Break.AsNoTracking().Where(b => holderIds.Contains(b.AbsenceId) && b.AnalyseToken == null);

    internal static IQueryable<Break> SealedBreaksOfAbsenceTypes(
        DataBaseContext context, IReadOnlyCollection<Guid> holderIds) =>
        BreaksOfAbsenceTypes(context, holderIds).Where(b => b.LockLevel != WorkLockLevel.None);

    internal static IQueryable<Break> OpenBreakSamples(
        DataBaseContext context, IReadOnlyCollection<Guid> holderIds, int sampleSize) =>
        BreaksOfAbsenceTypes(context, holderIds)
            .Where(b => b.LockLevel == WorkLockLevel.None)
            .OrderByDescending(b => b.CurrentDate)
            .ThenByDescending(b => b.StartTime)
            .Take(sampleSize);

    internal static IQueryable<string> MacroContent(DataBaseContext context, Guid macroId) =>
        context.Macro.AsNoTracking().Where(m => m.Id == macroId).Select(m => m.Content);
}
