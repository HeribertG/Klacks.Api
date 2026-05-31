// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, date- and level-aware match of a client's qualifications against a shift's MANDATORY
/// required qualifications on a given date. Returns one gap per unmet mandatory requirement,
/// classified as Missing (no such qualification), Expired (held at the required level but outside
/// its validity window) or InsufficientLevel (held and in-window but below the required level).
/// Non-mandatory requirements are ignored here (soft signal, not an eligibility blocker). No data
/// access — the caller loads the rows; this is a side-effect-free function.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public static class EligibilityMatcher
{
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

            var held = clientQualifications
                .Where(cq => cq.QualificationId == requirement.QualificationId)
                .ToList();

            if (held.Count == 0)
            {
                gaps.Add(new QualificationGap(requirement.QualificationId, QualificationGapReason.Missing, requirement.MinLevel));
                continue;
            }

            var inWindow = held.Where(cq => IsInWindow(cq, date)).ToList();

            if (inWindow.Any(cq => cq.Level >= requirement.MinLevel))
            {
                continue;
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

            gaps.Add(new QualificationGap(requirement.QualificationId, reason, requirement.MinLevel));
        }

        return gaps;
    }

    private static bool IsInWindow(ClientQualification qualification, DateOnly date)
        => (qualification.ValidFrom is null || qualification.ValidFrom <= date)
            && (qualification.ValidUntil is null || qualification.ValidUntil >= date);
}
