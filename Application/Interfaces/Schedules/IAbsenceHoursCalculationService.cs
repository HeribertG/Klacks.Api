// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Lays open the absence-hours calculation of one employee on one day by running the production pipeline
/// (effective contract data, macro input builder, macro of the absence type) on a transient entry.
/// </summary>
public interface IAbsenceHoursCalculationService
{
    /// <summary>
    /// Finds the absence type the question is about: by name or abbreviation in any language, otherwise the type of
    /// an entry the employee already has on that day, otherwise the vacation type.
    /// </summary>
    /// <param name="absenceName">Name or abbreviation as the caller supplied it; null or blank for the default</param>
    /// <param name="clientId">Employee whose existing entry may decide the type</param>
    /// <param name="date">Day of the existing entry</param>
    Task<(Absence? Absence, string? Error)> ResolveAbsenceAsync(
        string? absenceName, Guid clientId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the real calculation for the employee, day and absence type and returns every input, its source and the result.
    /// </summary>
    /// <param name="clientId">Employee</param>
    /// <param name="date">Calendar day of the absence</param>
    /// <param name="absence">Absence type whose macro computes the hours</param>
    /// <param name="paymentInterval">Payment interval to filter contracts with; null takes the company setting, as the Ui does</param>
    Task<AbsenceHoursCalculation> CalculateAsync(
        Guid clientId, DateOnly date, Absence absence, int? paymentInterval, CancellationToken cancellationToken = default);
}
