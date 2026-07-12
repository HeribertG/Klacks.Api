// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// 400 response body returned by the wizard Start endpoint when the request exceeds limits.
/// </summary>
/// <param name="Code">Machine-readable error code consumed by the frontend.</param>
/// <param name="Message">English fallback message for direct API callers.</param>
/// <param name="Agents">Submitted agent count.</param>
/// <param name="Shifts">Submitted shift count.</param>
/// <param name="MaxAgents">Enforced agent cap.</param>
/// <param name="MaxShifts">Enforced shift cap.</param>
public sealed record WizardLimitErrorResponse(
    string Code,
    string Message,
    int Agents,
    int Shifts,
    int MaxAgents,
    int MaxShifts);
