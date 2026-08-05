// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

/// <summary>
/// Builds the read-only capture report. Reads only: it never resolves an outcome or touches a capture,
/// so opening the report can never change what the future learner will be trained on.
/// </summary>
/// <param name="captureRepository">Source of the captured runs.</param>
/// <param name="trainingRepository">Source of the benchmark history.</param>
public sealed class GetWizardRunCaptureReportQueryHandler
    : IRequestHandler<GetWizardRunCaptureReportQuery, WizardRunCaptureReportDto>
{
    private const int RecentTrainingRunLimit = 50;

    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IWizardTrainingRepository _trainingRepository;

    public GetWizardRunCaptureReportQueryHandler(
        IWizardRunCaptureRepository captureRepository,
        IWizardTrainingRepository trainingRepository)
    {
        _captureRepository = captureRepository;
        _trainingRepository = trainingRepository;
    }

    public async Task<WizardRunCaptureReportDto> Handle(
        GetWizardRunCaptureReportQuery request, CancellationToken cancellationToken)
    {
        var captures = await _captureRepository.GetAllForReportAsync(
            request.From, request.Until, request.GroupId, cancellationToken);
        var best = await _trainingRepository.GetBestAsync(cancellationToken);
        var recent = await _trainingRepository.GetRecentAsync(RecentTrainingRunLimit, cancellationToken);

        return WizardRunCaptureReportBuilder.Build(captures, best, recent.Count);
    }
}
