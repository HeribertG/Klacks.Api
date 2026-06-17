// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One qualification gap surfaced to the frontend after a wizard run. Two kinds:
/// <c>UnfillableSlot</c> — a shift slot stayed empty because no available employee holds the required
/// qualification (AgentId/AgentName are null); <c>AssignedUnqualified</c> — an employee remained
/// assigned to a shift they are not qualified for (only produced by the harmonizers, which swap
/// rather than fill). Carries the qualification's localized name + emoji for direct rendering.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record QualificationGapDetail(
    QualificationGapKind Kind,
    Guid ShiftId,
    string ShiftName,
    DateOnly Date,
    Guid QualificationId,
    MultiLanguage QualificationName,
    string? QualificationEmoji,
    QualificationGapReason Reason,
    QualificationLevel RequiredMinLevel,
    QualificationGapSeverity Severity,
    Guid? AgentId = null,
    string? AgentName = null);
