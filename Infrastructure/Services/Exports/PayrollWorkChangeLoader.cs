// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the WorkChange rows of the payroll export (PayrollExportDataLoader) and of its completeness gate
/// (PayrollCompletenessGate): non-deleted, non-scenario changes on non-deleted, non-scenario works of an Employee or
/// ExternEmp in the period. Global query filters are ignored so a change on the work of a soft-deleted person (or naming
/// a soft-deleted substitute) is not lost; the soft-delete and scenario conditions of WorkChange and Work are applied
/// explicitly instead. Travel time, briefing and debriefing on an inner service of a container are left out
/// (WorkChangeOwnHours): the container counts that time.
/// @param context - Database context holding WorkChange, Work and Client
/// @param fromDate - Lower bound (inclusive) for Work.CurrentDate
/// @param untilDate - Upper bound (inclusive) for Work.CurrentDate
/// @param clientIds - Optional restriction: changes whose work belongs to, or whose substitute is, one of these persons
/// @param onlyLockLevel - Restricts to works of this lock level (Closed for the export), or null for every level
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.Exports;

public static class PayrollWorkChangeLoader
{
    public static async Task<List<PayrollWorkChangeRow>> LoadAsync(
        DataBaseContext context,
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid>? clientIds,
        WorkLockLevel? onlyLockLevel,
        CancellationToken cancellationToken)
    {
        var query = context.WorkChange
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(wc => !wc.IsDeleted
                && wc.AnalyseToken == null
                && wc.Work != null
                && !wc.Work.IsDeleted
                && wc.Work.AnalyseToken == null
                && (onlyLockLevel == null || wc.Work.LockLevel == onlyLockLevel)
                && wc.Work.CurrentDate >= fromDate
                && wc.Work.CurrentDate <= untilDate
                && wc.Work.Client != null
                && (wc.Work.Client.Type == EntityTypeEnum.Employee || wc.Work.Client.Type == EntityTypeEnum.ExternEmp))
            .Where(WorkChangeOwnHours.Counts);

        if (clientIds is not null)
        {
            var restricted = clientIds.ToList();
            query = query.Where(wc => restricted.Contains(wc.Work!.ClientId)
                || (wc.ReplaceClientId != null && restricted.Contains(wc.ReplaceClientId.Value)));
        }

        var rows = await query
            .Select(wc => new
            {
                wc.WorkId,
                WorkClientId = wc.Work!.ClientId,
                wc.ReplaceClientId,
                ReplaceClientIsPayrollPerson = wc.ReplaceClient != null
                    && (wc.ReplaceClient.Type == EntityTypeEnum.Employee || wc.ReplaceClient.Type == EntityTypeEnum.ExternEmp),
                wc.Work!.CurrentDate,
                wc.Type,
                wc.ChangeTime,
                wc.Surcharges,
                wc.Work!.LockLevel,
                wc.Work!.ShiftId,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new PayrollWorkChangeRow(
                r.WorkId,
                r.WorkClientId,
                r.ReplaceClientId,
                r.ReplaceClientIsPayrollPerson,
                r.CurrentDate,
                r.Type,
                r.ChangeTime,
                r.Surcharges,
                r.LockLevel,
                r.ShiftId))
            .ToList();
    }
}
