// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// The one roster priority order (row order) of every wizard stage: a hand-arranged user order is kept verbatim,
/// otherwise the employees are ranked by guaranteed hours descending and then by full-time hours descending, both scaled
/// to the planning range with member days only (<see cref="RosterRankHours"/>), so employees without a guarantee are
/// graded by their hours as well and weekly and monthly contracts compare fairly. Stand-ins always end up behind every
/// real person. All sorts are stable, so ties keep the incoming order.
/// </summary>
public static class RosterPriorityOrder
{
    /// <param name="baseOrder">Employees in the incoming list order</param>
    /// <param name="userDefined">True when the planner arranged the order by hand; it is then kept verbatim</param>
    /// <param name="rankHours">Sort keys of the automatic order, scaled to the planning range</param>
    /// <param name="agentId">Id of an employee, the key into <paramref name="rankHours"/></param>
    /// <param name="isStandIn">True for a stand-in (placeholder); stand-ins are ranked last</param>
    public static IReadOnlyList<T> Apply<T>(
        IReadOnlyList<T> baseOrder,
        bool userDefined,
        RosterRankHours rankHours,
        Func<T, Guid> agentId,
        Func<T, bool> isStandIn)
        => Apply(
            baseOrder,
            userDefined,
            a => rankHours.GuaranteedHoursOf(agentId(a)),
            a => rankHours.FullTimeOf(agentId(a)),
            isStandIn);

    /// <param name="baseOrder">Employees in the incoming list order</param>
    /// <param name="userDefined">True when the planner arranged the order by hand; it is then kept verbatim</param>
    /// <param name="guaranteedHours">Guaranteed hours of an employee (first sort key)</param>
    /// <param name="fullTime">Full-time hours of an employee (second sort key)</param>
    /// <param name="isStandIn">True for a stand-in (placeholder); stand-ins are ranked last</param>
    internal static IReadOnlyList<T> Apply<T>(
        IReadOnlyList<T> baseOrder,
        bool userDefined,
        Func<T, double> guaranteedHours,
        Func<T, double> fullTime,
        Func<T, bool> isStandIn)
    {
        IEnumerable<T> personOrder = userDefined
            ? baseOrder
            : baseOrder.OrderByDescending(guaranteedHours).ThenByDescending(fullTime);

        return personOrder.OrderBy(isStandIn).ToList();
    }
}
