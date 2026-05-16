// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
