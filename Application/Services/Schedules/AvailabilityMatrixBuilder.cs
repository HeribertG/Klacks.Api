// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Builds the (agent, shift, date) triples an agent must NOT receive because the agent is explicitly
/// unavailable during the shift's hours — the C2 availability gate, modelled on EligibilityMatrixBuilder
/// and reusing <see cref="AvailabilityMatcher"/>. Pure and additive: only agents with explicit
/// unavailability records produce triples (sparse = available, like a missing Break = not absent), so it
/// can only ever ADD blocks, never remove valid assignments. The triples merge into the wizards'
/// IneligibleAssignments set, where the existing per-move validators already enforce them. The DB load of
/// the availability rows + shift times is the caller's job (the context builders), keeping this testable.
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
                var dayRows = rows.Where(r => r.Date == slot.Date).ToList();
                if (dayRows.Count == 0)
                {
                    continue;
                }

                if (AvailabilityMatcher.IsExplicitlyUnavailable(dayRows, slot.Start, slot.End))
                {
                    ineligible.Add((agentId, slot.ShiftId, slot.Date));
                }
            }
        }

        return ineligible;
    }
}
