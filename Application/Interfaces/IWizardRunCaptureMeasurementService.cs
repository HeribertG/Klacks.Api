// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Measures the post-apply churn of one captured wizard run and persists the result. Loads the proposal set
/// and the post-apply events, delegates the correction-vs-event split to the pure churn calculator, and
/// writes CorrectionChurn/EventChurn/MeasuredAt plus the terminal outcome. A capture already resolved as
/// Rejected is never overwritten. The seal handler and the fallback timer are thin triggers over this service.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWizardRunCaptureMeasurementService
{
    /// <summary>
    /// Measures the given capture and stores the churn result under the supplied outcome (Accepted on seal,
    /// Expired on the fallback timer). No-op when the capture is already measured or already Rejected.
    /// </summary>
    /// <param name="capture">The capture to measure; its created-work links define the proposal set.</param>
    /// <param name="outcome">Terminal outcome to stamp together with the measurement.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MeasureAsync(WizardRunCapture capture, CaptureOutcome outcome, CancellationToken ct = default);
}
