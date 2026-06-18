// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request DTO for rejecting an AnalyseScenario with an optional structured reason.
/// </summary>
/// <param name="Reason">Structured rejection reason (optional; null = one-click reject without a reason)</param>
/// <param name="ReasonText">Optional free-text rejection note</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public class RejectAnalyseScenarioRequest
{
    public RejectReason? Reason { get; set; }

    public string? ReasonText { get; set; }
}
