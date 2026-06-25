// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Builds the (agent, shift, date) triples an agent must NOT receive because the agent is unavailable
/// during the shift's hours — the C2 availability gate, modelled on EligibilityMatrixBuilder and reusing
/// <see cref="AvailabilityMatcher"/>. Availability is opt-in PER DATE: a date with no record is open, a
/// date with available hours is positively constrained (only those hours are usable). The triples merge
/// into the wizards' IneligibleAssignments set, where the per-move validators enforce them; the context
/// builders drop the triples on days governed by a higher layer (Break / Keyword) before merging. The DB
/// load of the availability rows + shift times is the caller's job (the context builders), keeping this
/// testable.
/// </summary>
public static class AvailabilityMatrixBuilder
{
    public static IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> Build(
        IReadOnlyList<AvailabilityShiftSlot> slots,
        IReadOnlyDictionary<string, IReadOnlyList<ClientAvailability>> availabilityByAgent)
    {
        var ineligible = new HashSet<(string, Guid, DateOnly)>();
        if (slots.Count == 0 || availabilityByAgent.Count == 0)
        {
            return ineligible;
        }

        foreach (var (agentId, rows) in availabilityByAgent)
        {
            if (rows.Count == 0)
            {
                continue;
            }

            foreach (var slot in slots)
            {
                if (AvailabilityMatcher.IsUnavailable(rows, slot.Date, slot.Start, slot.End))
                {
                    ineligible.Add((agentId, slot.ShiftId, slot.Date));
                }
            }
        }

        return ineligible;
    }
}
