// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Ground-truth trigger for the preference-learner capture: when a period is sealed it measures every
/// unmeasured, non-rejected capture overlapping the sealed period (and group, when the seal was group-scoped)
/// and stamps them Accepted. The seal is the natural "this plan is final" signal. Runs post-commit and is
/// fully defensive: a failure on one capture is logged and swallowed so it can neither affect the committed
/// seal nor starve the sibling PeriodClosedEvent handlers (the dispatcher rethrows).
/// </summary>
/// <param name="captureRepository">Finds the unmeasured captures for the sealed period/group</param>
/// <param name="measurementService">Measures a single capture and persists its churn result</param>
/// <param name="logger">Logs the sweep result and per-capture failures</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class WizardRunCaptureMeasurementOnPeriodClosedHandler : IDomainEventHandler<PeriodClosedEvent>
{
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IWizardRunCaptureMeasurementService _measurementService;
    private readonly ILogger<WizardRunCaptureMeasurementOnPeriodClosedHandler> _logger;

    public WizardRunCaptureMeasurementOnPeriodClosedHandler(
        IWizardRunCaptureRepository captureRepository,
        IWizardRunCaptureMeasurementService measurementService,
        ILogger<WizardRunCaptureMeasurementOnPeriodClosedHandler> logger)
    {
        _captureRepository = captureRepository;
        _measurementService = measurementService;
        _logger = logger;
    }

    public async Task HandleAsync(PeriodClosedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var captures = await _captureRepository.GetUnmeasuredForSealAsync(
                domainEvent.StartDate, domainEvent.EndDate, domainEvent.GroupId, cancellationToken);

            if (captures.Count == 0)
            {
                return;
            }

            var measured = 0;
            foreach (var capture in captures)
            {
                try
                {
                    await _measurementService.MeasureAsync(capture, CaptureOutcome.Accepted, cancellationToken);
                    measured++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Post-apply measurement failed for WizardRunCapture {CaptureId} on seal of period {Start}..{End}; the seal is unaffected.",
                        capture.Id, domainEvent.StartDate, domainEvent.EndDate);
                }
            }

            _logger.LogInformation(
                "Sealed period {Start}..{End} (group {GroupId}) measured {Measured}/{Total} wizard-run capture(s) as Accepted.",
                domainEvent.StartDate, domainEvent.EndDate, domainEvent.GroupId, measured, captures.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Wizard-run capture measurement sweep failed for sealed period {Start}..{End}; the seal is committed and remains unaffected.",
                domainEvent.StartDate, domainEvent.EndDate);
        }
    }
}
