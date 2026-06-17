// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A single unmet qualification of a client for a shift on a date: which qualification,
/// why it is unmet (missing entirely / held but expired / held but below the required level),
/// the minimum level the shift requires, and whether the requirement is mandatory. The single
/// severity rule lives here: only a completely missing MANDATORY qualification is an Error
/// (a hard blocker); everything else — too-low level, expired, or any optional gap — is a Warning.
/// </summary>
/// <param name="QualificationId">The required qualification the client does not satisfy</param>
/// <param name="Reason">Why the requirement is unmet</param>
/// <param name="RequiredMinLevel">The minimum level the shift demands</param>
/// <param name="IsMandatory">Whether the shift marks this requirement mandatory</param>
public sealed record QualificationGap(
    Guid QualificationId,
    QualificationGapReason Reason,
    QualificationLevel RequiredMinLevel,
    bool IsMandatory = true)
{
    public QualificationGapSeverity Severity =>
        IsMandatory && Reason == QualificationGapReason.Missing
            ? QualificationGapSeverity.Error
            : QualificationGapSeverity.Warning;
}
