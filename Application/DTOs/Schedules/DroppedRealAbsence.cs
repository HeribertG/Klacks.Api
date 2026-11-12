// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A real absence an accept would delete although the scenario carries no absence of the same type for that person on
/// that day.
/// </summary>
/// <param name="ClientId">The absent person</param>
/// <param name="ClientName">Display name of the absent person (first name and name)</param>
/// <param name="Date">Day of the absence</param>
/// <param name="AbsenceId">Absence type of the real entry</param>
public sealed record DroppedRealAbsence(Guid ClientId, string ClientName, DateOnly Date, Guid AbsenceId);
