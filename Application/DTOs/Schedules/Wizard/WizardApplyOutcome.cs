// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Result of materialising a cached wizard plan through the compliance partition guardrail:
/// the Work ids that were actually created plus everything the partition reported.
/// </summary>
/// <param name="CreatedWorkIds">Ids of the Work entities created for the accepted placements</param>
/// <param name="ComplianceViolations">Warnings plus any Error entry a supervisor override let through</param>
/// <param name="SkippedPlacements">Placements the partition blocked, with the violating rule's key</param>
/// <param name="OverrideApplied">True when a supervisor override let blocked placements through</param>
public sealed record WizardApplyOutcome(
    IReadOnlyList<Guid> CreatedWorkIds,
    IReadOnlyList<ScheduleValidationNotificationDto> ComplianceViolations,
    IReadOnlyList<SkippedPlacementDto> SkippedPlacements,
    bool OverrideApplied);
