// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A single unmet mandatory qualification of a client for a shift on a date: which qualification,
/// why it is unmet (missing entirely / held but expired / held but below the required level) and
/// the minimum level the shift requires.
/// </summary>
/// <param name="QualificationId">The required qualification the client does not satisfy</param>
/// <param name="Reason">Why the requirement is unmet</param>
/// <param name="RequiredMinLevel">The minimum level the shift demands</param>
public sealed record QualificationGap(
    Guid QualificationId,
    QualificationGapReason Reason,
    QualificationLevel RequiredMinLevel);
