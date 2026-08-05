// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Measures the post-apply churn of one captured wizard run and persists the result. Loads the proposal set
/// and the post-apply events, delegates the correction-vs-event split to the pure churn calculator, and
/// writes CorrectionChurn/EventChurn/MeasuredAt plus the terminal outcome. A capture that already carries an
/// outcome is never overwritten. The seal handler and the fallback timer are thin triggers over this service and
/// call <see cref="IWizardRunCaptureMeasurementService.MeasureResolvedAsync"/>, which derives the outcome from
/// the linked scenario instead of assuming acceptance.
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

    /// <summary>
    /// Resolves the real fate of a capture from the state of its linked scenario and stamps the matching
    /// outcome. Only a direct apply or a promoted (accepted) scenario is measured as Accepted; a deleted or
    /// rejected scenario becomes Rejected without a churn measurement, and a scenario still undecided at seal
    /// time becomes Superseded. Captures without a scenario id are direct applies and are always real.
    /// </summary>
    /// <param name="capture">The capture whose fate is resolved.</param>
    /// <param name="periodSealed">True when triggered by a period seal, false on the fallback expiry timer.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MeasureResolvedAsync(WizardRunCapture capture, bool periodSealed, CancellationToken ct = default);
}
