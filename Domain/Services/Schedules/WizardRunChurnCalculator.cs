// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure post-apply churn calculation for a captured wizard run. Given the run's proposal cells and the set
/// of (agent, date) cells that carry a post-apply event (a new Break or a recovery-marked replacement), it
/// splits every corrected cell into a real correction (planner reworked the proposal) or an event-explained
/// change (reality moved), so a sickness-driven change never looks like a bad suggestion. A cell is corrected
/// when any of its works is soft-deleted or overlaid by a non-recovery WorkChange; when the same cell also
/// carries an event, the event explanation wins (it is counted as event churn, never twice). Holds no I/O —
/// the service resolves the raw facts and this class owns the event-cleaning and ratio math for isolated testing.
/// </summary>
namespace Klacks.Api.Domain.Services.Schedules;

public static class WizardRunChurnCalculator
{
    /// <summary>
    /// Computes correction- and event-churn for one captured run.
    /// </summary>
    /// <param name="proposalCells">The works the apply created, projected to churn cells with their delete/edit facts.</param>
    /// <param name="eventCells">(agent, date) cells with a post-apply Break or recovery marker; changes on these are events, not corrections.</param>
    public static WizardRunChurnResult Compute(
        IReadOnlyList<WizardRunProposalCell> proposalCells,
        IReadOnlySet<(Guid AgentId, DateOnly Date)> eventCells)
    {
        var distinct = proposalCells
            .GroupBy(c => (c.AgentId, c.Date, c.ShiftId, c.StartTime, c.EndTime))
            .Select(g => new
            {
                g.First().AgentId,
                g.First().Date,
                Corrected = g.Any(c => c.IsDeleted || c.IsOverlaid),
            })
            .ToList();

        var total = distinct.Count;
        if (total == 0)
        {
            return new WizardRunChurnResult(0.0, 0.0, 0, 0, 0);
        }

        var correctionCount = 0;
        var eventCount = 0;

        foreach (var cell in distinct)
        {
            if (!cell.Corrected)
            {
                continue;
            }

            if (eventCells.Contains((cell.AgentId, cell.Date)))
            {
                eventCount++;
            }
            else
            {
                correctionCount++;
            }
        }

        return new WizardRunChurnResult(
            (double)correctionCount / total,
            (double)eventCount / total,
            total,
            correctionCount,
            eventCount);
    }
}
