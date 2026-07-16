// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for WizardRunCapture rows and their created-work child links. Kept separate from
/// IBaseRepository because the capture is written as a unit (capture + work links) and read/updated
/// by scenario id and outcome rather than by primary key. Also owns the read model of the deferred
/// post-apply measurement: the proposal works (soft-deleted rows visible) and the post-apply event cells.
/// </summary>
/// <param name="AddAsync">Persists a capture together with the ids of the works it created.</param>
/// <param name="GetByScenarioIdAsync">Returns the capture linked to a scenario, or null.</param>
/// <param name="SetOutcomeAsync">Sets the terminal outcome on a capture.</param>
/// <param name="GetUnmeasuredForSealAsync">Unmeasured, non-rejected captures overlapping a sealed period/group; a group-scoped seal also recovers group-less direct-apply captures whose created works belong to the sealed group.</param>
/// <param name="GetUnmeasuredExpiredAsync">Unmeasured, non-rejected captures whose period ended before a cutoff.</param>
/// <param name="LoadMeasurementDataAsync">Loads the proposal cells (with their delete/overlay correction facts) and post-apply event cells for one capture.</param>
/// <param name="SetMeasurementAsync">Writes the churn result + measured-at + outcome, never overwriting a Rejected or already-measured capture (first-writer-wins).</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWizardRunCaptureRepository
{
    Task AddAsync(WizardRunCapture capture, IReadOnlyList<Guid> workIds, CancellationToken ct = default);

    Task<WizardRunCapture?> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct = default);

    Task SetOutcomeAsync(Guid captureId, CaptureOutcome outcome, CancellationToken ct = default);

    Task<IReadOnlyList<WizardRunCapture>> GetUnmeasuredForSealAsync(
        DateOnly periodFrom, DateOnly periodUntil, Guid? groupId, CancellationToken ct = default);

    Task<IReadOnlyList<WizardRunCapture>> GetUnmeasuredExpiredAsync(
        DateOnly periodEndedBefore, CancellationToken ct = default);

    Task<WizardRunMeasurementData> LoadMeasurementDataAsync(
        WizardRunCapture capture, string recoveryMarker, CancellationToken ct = default);

    Task SetMeasurementAsync(
        Guid captureId,
        double correctionChurn,
        double eventChurn,
        DateTime measuredAt,
        CaptureOutcome outcome,
        CancellationToken ct = default);
}
