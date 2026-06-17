// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, date- and level-aware match of a client's qualifications against a shift's required
/// qualifications on a given date. Each unmet requirement is classified as Missing (no such
/// qualification), Expired (held at the required level but outside its validity window) or
/// InsufficientLevel (held and in-window but below the required level). <see cref="FindMandatoryGaps"/>
/// considers only mandatory requirements (the pre-commit guardrail's blocker set); <see cref="FindGaps"/>
/// considers all requirements and carries the IsMandatory flag so callers can derive severity.
/// No data access — the caller loads the rows; this is a side-effect-free function.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public static class EligibilityMatcher
{
    /// <summary>Mandatory-only gaps (unchanged contract; used by the Klacksy pre-commit guardrail).</summary>
    public static IReadOnlyList<QualificationGap> FindMandatoryGaps(
        IReadOnlyList<ShiftRequiredQualification> requirements,
        IReadOnlyList<ClientQualification> clientQualifications,
        DateOnly date)
    {
        var gaps = new List<QualificationGap>();
        foreach (var requirement in requirements)
        {
            if (!requirement.IsMandatory)
            {
                continue;
            }

            var gap = EvaluateRequirement(requirement, clientQualifications, date);
            if (gap is not null)
            {
                gaps.Add(gap);
            }
        }

        return gaps;
    }

    /// <summary>All gaps (mandatory and optional), each carrying IsMandatory so the caller can derive
    /// the severity (Error only for a completely missing mandatory qualification, Warning otherwise).</summary>
    public static IReadOnlyList<QualificationGap> FindGaps(
        IReadOnlyList<ShiftRequiredQualification> requirements,
        IReadOnlyList<ClientQualification> clientQualifications,
        DateOnly date)
    {
        var gaps = new List<QualificationGap>();
        foreach (var requirement in requirements)
        {
            var gap = EvaluateRequirement(requirement, clientQualifications, date);
            if (gap is not null)
            {
                gaps.Add(gap);
            }
        }

        return gaps;
    }

    private static QualificationGap? EvaluateRequirement(
        ShiftRequiredQualification requirement,
        IReadOnlyList<ClientQualification> clientQualifications,
        DateOnly date)
    {
        var held = clientQualifications
            .Where(cq => cq.QualificationId == requirement.QualificationId)
            .ToList();

        if (held.Count == 0)
        {
            return new QualificationGap(requirement.QualificationId, QualificationGapReason.Missing, requirement.MinLevel, requirement.IsMandatory);
        }

        var inWindow = held.Where(cq => IsInWindow(cq, date)).ToList();

        if (inWindow.Any(cq => cq.Level >= requirement.MinLevel))
        {
            return null;
        }

        QualificationGapReason reason;
        if (inWindow.Count > 0)
        {
            reason = QualificationGapReason.InsufficientLevel;
        }
        else if (held.Any(cq => cq.Level >= requirement.MinLevel))
        {
            reason = QualificationGapReason.Expired;
        }
        else
        {
            reason = QualificationGapReason.Missing;
        }

        return new QualificationGap(requirement.QualificationId, reason, requirement.MinLevel, requirement.IsMandatory);
    }

    private static bool IsInWindow(ClientQualification qualification, DateOnly date)
        => (qualification.ValidFrom is null || qualification.ValidFrom <= date)
            && (qualification.ValidUntil is null || qualification.ValidUntil >= date);
}
