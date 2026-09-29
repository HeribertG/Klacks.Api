// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The client scope of the schedule view for a period: no customers, and only clients whose
/// membership overlaps the period. Shared by the schedule client list and the planning-agent
/// resolution so the agents a wizard plans are exactly the rows the schedule shows.
/// </summary>
/// <param name="query">Client query to restrict</param>
/// <param name="startDate">First day of the period, inclusive</param>
/// <param name="endDate">Last day of the period, inclusive</param>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Services.Clients;

public static class ScheduleClientScope
{
    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    public static IQueryable<Client> ActiveInPeriod(IQueryable<Client> query, DateOnly startDate, DateOnly endDate)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = endDate.ToDateTime(EndOfDay, DateTimeKind.Utc);

        return query
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .Where(c => c.Membership != null &&
                        c.Membership.ValidFrom <= endDateTime &&
                        (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startDateTime));
    }
}
