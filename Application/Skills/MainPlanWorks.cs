// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Narrows the Works of one client and date range to the entries a chat command on "the employee's shifts" means:
/// the main plan (no scenario token) and the container parents. WorkRepository.GetByClientAndDateRangeAsync also
/// returns container children and the rows of every scenario, which a name-and-date lookup must never pick up.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Skills;

internal static class MainPlanWorks
{
    public static List<Work> ParentsOnly(IEnumerable<Work> works) =>
        works
            .Where(work => work.AnalyseToken == null && work.ParentWorkId == null)
            .OrderBy(work => work.CurrentDate)
            .ThenBy(work => work.StartTime)
            .ToList();
}
