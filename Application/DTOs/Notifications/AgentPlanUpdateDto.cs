// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Notifications;

public class AgentPlanUpdateDto
{
    public Guid PlanId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int CurrentStepIndex { get; set; }

    public int TotalSteps { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
