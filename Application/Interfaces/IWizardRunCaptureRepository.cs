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
/// <param name="SupersedeOpenDirectCapturesAsync">Marks every still-open direct-apply capture of the same engine overlapping the period as Superseded and returns how many were stamped.</param>
/// <param name="GetUnmeasuredForSealAsync">Captures without any outcome yet, overlapping a sealed period/group; a group-scoped seal also recovers group-less direct-apply captures whose created works belong to the sealed group.</param>
/// <param name="GetUnmeasuredExpiredAsync">Captures without any outcome yet whose period ended before a cutoff.</param>
/// <param name="GetScenarioStateAsync">Returns status and delete flag of a scenario, soft-deleted rows included; null when the row no longer exists.</param>
/// <param name="LoadMeasurementDataAsync">Loads the proposal cells (with their delete/overlay correction facts) and post-apply event cells for one capture.</param>
/// <param name="SetMeasurementAsync">Writes the churn result + measured-at + outcome, never overwriting a capture that already carries any outcome or measurement (first-writer-wins).</param>

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

    Task<CaptureScenarioState?> GetScenarioStateAsync(Guid scenarioId, CancellationToken ct = default);

    /// <summary>
    /// Every capture in the requested window, for the read-only report. Unlike the sweep queries this
    /// one deliberately includes resolved and unmeasured captures alike - the report is about the
    /// distribution of outcomes, so filtering any of them out would bias it.
    /// </summary>
    /// <param name="from">Only captures whose period ends on or after this day; null for no lower bound.</param>
    /// <param name="until">Only captures whose period starts on or before this day; null for no upper bound.</param>
    /// <param name="groupId">Only captures of this group; null for every group.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<WizardRunCapture>> GetAllForReportAsync(
        DateOnly? from, DateOnly? until, Guid? groupId, CancellationToken ct = default);

    Task<int> SupersedeOpenDirectCapturesAsync(
        Guid newCaptureId,
        WizardEngine engine,
        DateOnly periodFrom,
        DateOnly periodUntil,
        CancellationToken ct = default);

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
