// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One Stage-1 escalation entry: an agent had to be assigned despite a soft-rule violation
/// (e.g. MaxWorkDays exceeded) because no Stage-1-clean alternative existed.
/// </summary>
/// <param name="AgentId">Agent that received the slot</param>
/// <param name="Date">Date of the affected slot</param>
/// <param name="RuleName">Stage-1 rule that was relaxed</param>
/// <param name="Hint">Human-readable explanation for UI display</param>
public sealed record WizardEscalationDto(string AgentId, string Date, string RuleName, string Hint);
