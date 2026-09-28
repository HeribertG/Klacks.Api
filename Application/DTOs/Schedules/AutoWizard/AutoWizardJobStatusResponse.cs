// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <summary>
/// Response of the auto-wizard job status endpoint. Lets clients that missed SignalR events
/// (reconnect window) recover the job outcome.
/// </summary>
/// <param name="Status">One of the WizardJobStatusValues constants.</param>
/// <param name="Result">Final result when the job completed, otherwise null.</param>
/// <param name="Reason">Failure reason when the job failed, otherwise null.</param>
public sealed record AutoWizardJobStatusResponse(
    string Status,
    AutoWizardJobResultDto? Result,
    string? Reason);
