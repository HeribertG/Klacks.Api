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
        DateOnly date,
        bool expiredMandatoryBlocks = false)
    {
        var gaps = new List<QualificationGap>();
        foreach (var requirement in requirements)
        {
            if (!requirement.IsMandatory)
            {
                continue;
            }

            var gap = EvaluateRequirement(requirement, clientQualifications, date, expiredMandatoryBlocks);
            if (gap is not null)
            {
                gaps.Add(gap);
            }
        }

        return gaps;
    }

    /// <summary>All gaps (mandatory and optional), each carrying IsMandatory so the caller can derive
    /// the severity (Error only for a completely missing mandatory qualification, Warning otherwise;
    /// also Error for an expired mandatory qualification when <paramref name="expiredMandatoryBlocks"/>
    /// is set — the opt-in QUALIFICATION_EXPIRED_MANDATORY_BLOCKS region-setup behavior).</summary>
    public static IReadOnlyList<QualificationGap> FindGaps(
        IReadOnlyList<ShiftRequiredQualification> requirements,
        IReadOnlyList<ClientQualification> clientQualifications,
        DateOnly date,
        bool expiredMandatoryBlocks = false)
    {
        var gaps = new List<QualificationGap>();
        foreach (var requirement in requirements)
        {
            var gap = EvaluateRequirement(requirement, clientQualifications, date, expiredMandatoryBlocks);
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
        DateOnly date,
        bool expiredMandatoryBlocks)
    {
        var held = clientQualifications
            .Where(cq => cq.QualificationId == requirement.QualificationId)
            .ToList();

        if (held.Count == 0)
        {
            return new QualificationGap(requirement.QualificationId, QualificationGapReason.Missing, requirement.MinLevel, requirement.IsMandatory, expiredMandatoryBlocks);
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

        return new QualificationGap(requirement.QualificationId, reason, requirement.MinLevel, requirement.IsMandatory, expiredMandatoryBlocks);
    }

    /// <summary>
    /// Proactive QUALIFICATION_EXPIRY_WARNING_DAYS check: mandatory requirements the client currently
    /// satisfies (held, in-window, at the required level) whose validity ends within
    /// <paramref name="warningDays"/> of <paramref name="date"/>. Never a blocker — a fully separate,
    /// additive signal to the Expired/Missing gaps above (a qualification cannot be both "still valid"
    /// and "expired" for the same date).
    /// </summary>
    public static IReadOnlyList<QualificationExpiryWarning> FindExpiringSoon(
        IReadOnlyList<ShiftRequiredQualification> requirements,
        IReadOnlyList<ClientQualification> clientQualifications,
        DateOnly date,
        int warningDays)
    {
        if (warningDays <= 0)
        {
            return [];
        }

        var horizon = date.AddDays(warningDays);
        var warnings = new List<QualificationExpiryWarning>();

        foreach (var requirement in requirements.Where(r => r.IsMandatory))
        {
            var satisfying = clientQualifications
                .Where(cq => cq.QualificationId == requirement.QualificationId
                    && cq.Level >= requirement.MinLevel
                    && IsInWindow(cq, date)
                    && cq.ValidUntil.HasValue
                    && cq.ValidUntil.Value >= date
                    && cq.ValidUntil.Value <= horizon)
                .OrderBy(cq => cq.ValidUntil)
                .FirstOrDefault();

            if (satisfying is not null)
            {
                warnings.Add(new QualificationExpiryWarning(requirement.QualificationId, satisfying.ValidUntil!.Value));
            }
        }

        return warnings;
    }

    private static bool IsInWindow(ClientQualification qualification, DateOnly date)
        => (qualification.ValidFrom is null || qualification.ValidFrom <= date)
            && (qualification.ValidUntil is null || qualification.ValidUntil >= date);
}
