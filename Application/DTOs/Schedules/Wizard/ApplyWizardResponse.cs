// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Response from Apply: the created Work ids plus the compliance partition report.
/// </summary>
/// <param name="CreatedWorkIds">Ids of the Work entities created for the accepted placements.</param>
/// <param name="ComplianceViolations">Warnings plus any Error entry a supervisor override let through.</param>
/// <param name="SkippedPlacements">Placements the compliance partition blocked.</param>
/// <param name="OverrideApplied">True when a supervisor override let blocked placements through.</param>
public sealed record ApplyWizardResponse(
    IReadOnlyList<Guid> CreatedWorkIds,
    IReadOnlyList<ScheduleValidationNotificationDto> ComplianceViolations,
    IReadOnlyList<SkippedPlacementDto> SkippedPlacements,
    bool OverrideApplied);
