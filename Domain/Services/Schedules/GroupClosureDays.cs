// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure rule for company holidays of a group subtree: a day is a closure day when the subtree has at least
/// one active member that day and every active member is away the whole day. A day without any active
/// member is never a closure day - an empty team says nothing about the business being closed.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class GroupClosureDays
{
    /// <summary>
    /// The closure days within the inclusive range.
    /// </summary>
    /// <param name="windows">Membership windows of the subtree; one employee may have several</param>
    /// <param name="absences">Full-day absences of the subtree's employees</param>
    /// <param name="from">First day of the range</param>
    /// <param name="until">Last day of the range</param>
    public static IReadOnlySet<DateOnly> Compute(
        IReadOnlyCollection<GroupMembershipWindow> windows,
        IReadOnlyCollection<ClientFullDayAbsence> absences,
        DateOnly from,
        DateOnly until)
    {
        var closureDays = new HashSet<DateOnly>();
        if (windows.Count == 0 || until < from)
        {
            return closureDays;
        }

        var absent = absences
            .Select(absence => (absence.ClientId, absence.Date))
            .ToHashSet();

        for (var day = from; day <= until; day = day.AddDays(1))
        {
            var activeMembers = windows
                .Where(window => window.IsActiveOn(day))
                .Select(window => window.ClientId)
                .Distinct()
                .ToList();

            if (activeMembers.Count > 0 && activeMembers.All(clientId => absent.Contains((clientId, day))))
            {
                closureDays.Add(day);
            }
        }

        return closureDays;
    }
}
