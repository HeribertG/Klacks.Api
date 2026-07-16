// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IWizardRunCaptureMeasurementService"/>. Loads the capture's proposal set and post-apply
/// events from the repository, delegates the correction-vs-event split to the pure
/// <see cref="WizardRunChurnCalculator"/>, and persists the result. Kept thin: the seal handler and the
/// fallback timer only decide <em>which</em> captures to measure and under which outcome.
/// </summary>
/// <param name="captureRepository">Reads the measurement data and writes the churn result</param>
/// <param name="logger">Logs the measured churn and skips</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class WizardRunCaptureMeasurementService : IWizardRunCaptureMeasurementService
{
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly ILogger<WizardRunCaptureMeasurementService> _logger;

    public WizardRunCaptureMeasurementService(
        IWizardRunCaptureRepository captureRepository,
        ILogger<WizardRunCaptureMeasurementService> logger)
    {
        _captureRepository = captureRepository;
        _logger = logger;
    }

    public async Task MeasureAsync(WizardRunCapture capture, CaptureOutcome outcome, CancellationToken ct = default)
    {
        if (capture.MeasuredAt is not null || capture.Outcome == CaptureOutcome.Rejected)
        {
            return;
        }

        var data = await _captureRepository.LoadMeasurementDataAsync(capture, RecoveryMarkers.WorkChangeSource, ct);
        var result = WizardRunChurnCalculator.Compute(data.ProposalCells, data.EventCells);

        await _captureRepository.SetMeasurementAsync(
            capture.Id, result.CorrectionChurn, result.EventChurn, DateTime.UtcNow, outcome, ct);

        _logger.LogInformation(
            "Measured WizardRunCapture {CaptureId} ({Engine}/{ApplyKind}) as {Outcome}: correctionChurn {Correction:F3}, eventChurn {Event:F3} over {Cells} proposal cell(s).",
            capture.Id, capture.Engine, capture.ApplyKind, outcome, result.CorrectionChurn, result.EventChurn, result.ProposalCellCount);
    }
}
