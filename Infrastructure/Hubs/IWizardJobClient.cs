// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client contract for wizard job progress streams.
/// </summary>
public interface IWizardJobClient
{
    Task OnProgress(WizardJobProgressDto progress);

    Task OnCompleted(WizardJobResultDto result);

    Task OnCancelled();

    Task OnFailed(string reason);
}

/// <summary>
/// Progress snapshot broadcast per generation.
/// </summary>
/// <param name="JobId">Unique job identifier</param>
/// <param name="Generation">Current generation number (1-based)</param>
/// <param name="MaxGenerations">Configured upper bound</param>
/// <param name="BestHardViolations">Stage-0 value of the current best scenario</param>
/// <param name="BestStage1Completion">Stage-1 completion rate (0..1)</param>
/// <param name="BestStage2Score">Stage-2 weighted coverage (0..1)</param>
/// <param name="EarlyStopping">True if the loop is about to terminate</param>
public sealed record WizardJobProgressDto(
    Guid JobId,
    int Generation,
    int MaxGenerations,
    int BestHardViolations,
    double BestStage1Completion,
    double BestStage2Score,
    bool EarlyStopping);

/// <summary>
/// Final result broadcast when the GA loop finishes normally.
/// </summary>
/// <param name="JobId">Unique job identifier</param>
/// <param name="FinalHardViolations">Stage-0 value of the final best scenario</param>
/// <param name="FinalStage1Completion">Stage-1 completion rate</param>
/// <param name="TokenCount">Number of tokens in the final scenario</param>
public sealed record WizardJobResultDto(
    Guid JobId,
    int FinalHardViolations,
    double FinalStage1Completion,
    int TokenCount);
