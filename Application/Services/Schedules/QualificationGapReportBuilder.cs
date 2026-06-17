// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the frontend qualification-gap report from a precomputed <see cref="EligibilityMatrix"/>
/// and a finished plan. <c>BuildUnfillableSlots</c> (Wizard 1): shift slots that stayed empty because
/// no available employee is eligible. <c>BuildAssignedUnqualified</c> (Wizard 2/3): assignments kept
/// in the final plan whose agent is not eligible for the shift (the harmonizers swap, never fill).
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

public static class QualificationGapReportBuilder
{
    /// <summary>
    /// Flattens a harmony bitmap to its work assignments (agent, display name, shift, date), used by
    /// the harmonizers (Wizard 2/3) both to derive the eligibility slots and to scan the final plan.
    /// </summary>
    public static List<(string AgentId, string AgentName, Guid ShiftId, DateOnly Date)> ExtractBitmapAssignments(HarmonyBitmap bitmap)
    {
        var assignments = new List<(string, string, Guid, DateOnly)>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            var agent = bitmap.Rows[r];
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                if (cell.ShiftRefId is Guid shiftId && shiftId != Guid.Empty)
                {
                    assignments.Add((agent.Id, agent.DisplayName, shiftId, bitmap.Days[d]));
                }
            }
        }

        return assignments;
    }

    /// <summary>
    /// Wizard 1: reports each empty shift slot for which no agent is eligible. A slot left empty while
    /// at least one eligible agent exists is ordinary under-supply (other constraints) and not reported.
    /// </summary>
    public static IReadOnlyList<QualificationGapDetail> BuildUnfillableSlots(
        EligibilityMatrix matrix,
        IReadOnlyList<CoreShift> shifts,
        IReadOnlyList<CoreAgent> agents,
        IReadOnlyList<CoreToken> finalTokens)
    {
        if (matrix.Ineligible.Count == 0)
        {
            return [];
        }

        var assignedCount = new Dictionary<(Guid ShiftId, DateOnly Date), int>();
        foreach (var token in finalTokens)
        {
            if (token.ShiftRefId == Guid.Empty)
            {
                continue;
            }

            var occupied = (token.ShiftRefId, token.Date);
            assignedCount[occupied] = assignedCount.GetValueOrDefault(occupied) + 1;
        }

        var capacity = new Dictionary<(Guid ShiftId, DateOnly Date), int>();
        var shiftNames = new Dictionary<Guid, string>();
        foreach (var shift in shifts)
        {
            if (!Guid.TryParse(shift.Id, out var shiftId)
                || !DateOnly.TryParseExact(shift.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                continue;
            }

            var key = (shiftId, date);
            capacity[key] = capacity.GetValueOrDefault(key) + Math.Max(1, shift.RequiredAssignments);
            shiftNames.TryAdd(shiftId, shift.Name);
        }

        var report = new List<QualificationGapDetail>();
        foreach (var (key, cap) in capacity)
        {
            if (assignedCount.GetValueOrDefault(key) >= cap)
            {
                continue;
            }

            var anyEligible = agents.Any(a => !matrix.Ineligible.Contains((a.Id, key.ShiftId, key.Date)));
            if (anyEligible)
            {
                continue;
            }

            var distinctGaps = new Dictionary<Guid, QualificationGap>();
            foreach (var agent in agents)
            {
                if (!matrix.Gaps.TryGetValue((agent.Id, key.ShiftId, key.Date), out var gaps))
                {
                    continue;
                }

                foreach (var gap in gaps)
                {
                    distinctGaps.TryAdd(gap.QualificationId, gap);
                }
            }

            var shiftName = shiftNames.GetValueOrDefault(key.ShiftId, string.Empty);
            foreach (var gap in distinctGaps.Values)
            {
                // A slot is unfillable only because of hard (Error) gaps — warning gaps describe
                // agents who could in fact have taken it, so they are not the reason it stayed empty.
                if (gap.Severity != QualificationGapSeverity.Error)
                {
                    continue;
                }

                report.Add(BuildDetail(QualificationGapKind.UnfillableSlot, matrix, key.ShiftId, shiftName, key.Date, gap));
            }
        }

        return report;
    }

    /// <summary>
    /// Wizard 2/3: reports each final assignment whose agent is not eligible for the shift. The
    /// harmonizers swap rather than fill, so this scans the produced plan against the matrix —
    /// pre-existing unqualified cells the harmonizer left untouched surface here too.
    /// </summary>
    public static IReadOnlyList<QualificationGapDetail> BuildAssignedUnqualified(
        EligibilityMatrix matrix,
        IEnumerable<(string AgentId, string AgentName, Guid ShiftId, DateOnly Date)> assignments)
    {
        // Scan against Gaps (not Ineligible): a warning-level gap is not in Ineligible but must
        // still be reported on the assigned agent.
        if (matrix.Gaps.Count == 0)
        {
            return [];
        }

        var report = new List<QualificationGapDetail>();
        foreach (var assignment in assignments)
        {
            if (assignment.ShiftId == Guid.Empty)
            {
                continue;
            }

            var key = (assignment.AgentId, assignment.ShiftId, assignment.Date);
            if (!matrix.Gaps.TryGetValue(key, out var gaps))
            {
                continue;
            }

            var shiftName = matrix.ShiftNames.GetValueOrDefault(assignment.ShiftId, string.Empty);
            var agentId = Guid.TryParse(assignment.AgentId, out var parsedAgentId) ? parsedAgentId : (Guid?)null;
            foreach (var gap in gaps)
            {
                report.Add(BuildDetail(
                    QualificationGapKind.AssignedUnqualified, matrix, assignment.ShiftId, shiftName, assignment.Date, gap, agentId, assignment.AgentName));
            }
        }

        return report;
    }

    private static QualificationGapDetail BuildDetail(
        QualificationGapKind kind,
        EligibilityMatrix matrix,
        Guid shiftId,
        string shiftName,
        DateOnly date,
        QualificationGap gap,
        Guid? agentId = null,
        string? agentName = null)
    {
        var info = matrix.QualificationInfo.GetValueOrDefault(gap.QualificationId);
        return new QualificationGapDetail(
            Kind: kind,
            ShiftId: shiftId,
            ShiftName: shiftName,
            Date: date,
            QualificationId: gap.QualificationId,
            QualificationName: info?.Name ?? new MultiLanguage(),
            QualificationEmoji: info?.Emoji,
            Reason: gap.Reason,
            RequiredMinLevel: gap.RequiredMinLevel,
            Severity: gap.Severity,
            AgentId: agentId,
            AgentName: agentName);
    }
}
