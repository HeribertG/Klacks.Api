// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The cell a booking diagnosis checks: one employee, one shift, one day, in the real plan or in one scenario.
/// </summary>
/// <param name="ClientId">Employee who should get the shift</param>
/// <param name="ShiftId">Shift to book</param>
/// <param name="Date">Day of the booking</param>
/// <param name="AnalyseToken">Scenario token; null checks the real plan</param>
public sealed record WorkBookingDiagnosisRequest(Guid ClientId, Guid ShiftId, DateOnly Date, Guid? AnalyseToken);
