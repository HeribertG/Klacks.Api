// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether the period of a payroll export is complete: every Work and Break of each person is Closed, every
/// day of the person with an entry or an active group membership is locked by a period-close seal, and no export of
/// another overlapping period exists for the person.
/// @param from - First day of the period (inclusive)
/// @param until - Last day of the period (inclusive)
/// @param clientIds - Optional restriction to these persons (supplementary export); null checks every person
/// </summary>
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPayrollCompletenessGate
{
    Task<PayrollCompletenessResult> CheckAsync(
        DateOnly from,
        DateOnly until,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default);
}
