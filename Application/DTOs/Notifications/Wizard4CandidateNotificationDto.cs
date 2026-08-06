// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

/// <summary>
/// A background-optimiser candidate appeared, was replaced or timed out. Pushed to the people looking
/// at the real schedule of that group, so the scenario list reflects what is actually there instead of
/// only updating on the next manual reload.
/// </summary>
/// <param name="ScenarioId">Scenario the change is about; null when several were affected at once.</param>
/// <param name="GroupId">Group the candidate belongs to; null for a group-less candidate.</param>
/// <param name="FromDate">Start of the candidate's period.</param>
/// <param name="UntilDate">End of the candidate's period.</param>
/// <param name="ChangeKind">Created, Superseded or Expired.</param>
public sealed record Wizard4CandidateNotificationDto(
    Guid? ScenarioId,
    Guid? GroupId,
    DateOnly FromDate,
    DateOnly UntilDate,
    string ChangeKind);
