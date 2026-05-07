// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One planned assignment token sent in the final result.
/// </summary>
/// <param name="AgentId">Client GUID as string</param>
/// <param name="ShiftId">Shift GUID as string</param>
/// <param name="Date">ISO date (yyyy-MM-dd)</param>
/// <param name="StartTime">HH:mm</param>
/// <param name="EndTime">HH:mm</param>
/// <param name="Hours">Duration in hours</param>
public sealed record WizardTokenDto(
    string AgentId,
    string ShiftId,
    string Date,
    string StartTime,
    string EndTime,
    decimal Hours);
