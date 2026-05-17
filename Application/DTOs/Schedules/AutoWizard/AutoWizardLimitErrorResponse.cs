// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <param name="Code">Machine-readable error code consumed by the frontend.</param>
/// <param name="Message">English fallback message for direct API callers.</param>
/// <param name="Agents">Submitted agent count.</param>
/// <param name="Shifts">Submitted shift count.</param>
/// <param name="PeriodDays">Computed period length in days.</param>
/// <param name="MaxAgents">Enforced agent cap.</param>
/// <param name="MaxShifts">Enforced shift cap.</param>
/// <param name="MaxSlotProduct">Enforced agents x shifts x periodDays cap.</param>
public sealed record AutoWizardLimitErrorResponse(
    string Code,
    string Message,
    int Agents,
    int Shifts,
    int PeriodDays,
    int MaxAgents,
    int MaxShifts,
    int MaxSlotProduct);
