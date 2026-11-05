// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared filter that enforces the scheduling precedence of a Break over Availability: it removes
/// availability-ineligible (agent, shift, date) triples on any (agent, date) the caller passes (the days of a Break).
/// Keyword days are not passed since 2026-11-05: availability stays hard there, a keyword is only a wish. Qualification
/// ineligibility is merged separately by the context builders and must never be passed here.
/// </summary>

namespace Klacks.Api.Application.Services.Schedules;

public static class AvailabilitySuppression
{
    /// <summary>
    /// Returns the availability set with every triple dropped whose (AgentId, Date) is in
    /// <paramref name="governedDays"/>. Returns the input unchanged when either set is empty.
    /// </summary>
    public static IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> RemoveGovernedDays(
        IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> availability,
        IReadOnlySet<(string AgentId, DateOnly Date)> governedDays)
    {
        if (availability.Count == 0 || governedDays.Count == 0)
        {
            return availability;
        }

        return availability
            .Where(triple => !governedDays.Contains((triple.AgentId, triple.Date)))
            .ToHashSet();
    }
}
