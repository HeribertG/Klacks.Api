// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Projections of works and breaks into the snapshots the accept day-lock plan, the promote and the staleness check
/// compare, so all three read exactly the same columns.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal static class AcceptRowSnapshots
{
    public static readonly Expression<Func<Work, AcceptWorkSnapshot>> Work = w => new AcceptWorkSnapshot(
        w.Id, w.ClientId, w.CurrentDate, w.ShiftId, w.StartTime, w.EndTime, w.WorkTime, w.Information,
        w.LockLevel, w.ParentWorkId, w.ClonedFromWorkId, w.AnalyseToken, w.IsDeleted);

    public static readonly Expression<Func<Break, AcceptBreakSnapshot>> Break = b => new AcceptBreakSnapshot(
        b.Id, b.ClientId, b.CurrentDate, b.AbsenceId, b.StartTime, b.EndTime, b.LockLevel, b.ParentWorkId);
}
