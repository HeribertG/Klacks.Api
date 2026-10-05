// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Explains for one person and one date whether a public holiday earns the holiday time surcharge and whether it
/// raises the holiday-work warning, built from the same services payroll and the warning use.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IHolidayOutcomeDiagnosisService
{
    /// <summary>
    /// Diagnoses the holiday outcome; the caller has already checked that the person is visible.
    /// </summary>
    /// <param name="clientId">The employee</param>
    /// <param name="clientName">Display name carried into the warning evaluation</param>
    /// <param name="date">The calendar day in question</param>
    /// <param name="cancellationToken">Cancels the reads</param>
    Task<HolidayOutcomeDiagnosis> DiagnoseAsync(Guid clientId, string clientName, DateOnly date, CancellationToken cancellationToken = default);
}
