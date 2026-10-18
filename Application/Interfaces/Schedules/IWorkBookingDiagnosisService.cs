// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Explains, without writing anything, why one employee cannot get one shift on one day right now: the checks of the
/// direct booking path in its order, plus what the assistant and the schedule grid refuse on top.
/// </summary>
public interface IWorkBookingDiagnosisService
{
    /// <param name="request">The cell to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkBookingDiagnosis> DiagnoseAsync(WorkBookingDiagnosisRequest request, CancellationToken cancellationToken);
}
