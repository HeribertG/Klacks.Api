// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared filter that enforces the scheduling precedence (Break / Keyword over Availability): it removes
/// availability-ineligible (agent, shift, date) triples on any (agent, date) ruled by a higher layer, so
/// availability — the weakest layer — never overrides a Break or a Keyword command. Qualification
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
