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
/// One planned assignment token sent in the final result.
/// </summary>
/// <param name="AgentId">Client GUID as string</param>
/// <param name="ShiftId">Shift GUID as string</param>
/// <param name="Date">ISO date (yyyy-MM-dd)</param>
/// <param name="StartTime">HH:mm</param>
/// <param name="EndTime">HH:mm</param>
/// <param name="Hours">Duration in hours</param>
public sealed record WizardTokenDto(
    string AgentId,
    string ShiftId,
    string Date,
    string StartTime,
    string EndTime,
    decimal Hours);

/// <summary>
/// Auction telemetry per planned token (one entry per non-locked token).
/// Round 1 = clean award, Round 2 = Stage-1 escalation accepted.
/// </summary>
/// <param name="AgentId">Winner agent id</param>
/// <param name="Date">Slot date</param>
/// <param name="ShiftId">Slot shift id</param>
/// <param name="Round">Auction round (1 or 2)</param>
/// <param name="WinningScore">Fuzzy bid score that won the slot</param>
/// <param name="FiredRules">Top-3 rule names that contributed (Phase 2 fuzzy)</param>
public sealed record WizardAuctionAwardDto(
    string AgentId,
    string Date,
    string ShiftId,
    int Round,
    double WinningScore,
    IReadOnlyList<string> FiredRules);

/// <summary>
/// One Stage-1 escalation entry: an agent had to be assigned despite a soft-rule violation
/// (e.g. MaxWorkDays exceeded) because no Stage-1-clean alternative existed.
/// </summary>
/// <param name="AgentId">Agent that received the slot</param>
/// <param name="Date">Date of the affected slot</param>
/// <param name="RuleName">Stage-1 rule that was relaxed</param>
/// <param name="Hint">Human-readable explanation for UI display</param>
public sealed record WizardEscalationDto(string AgentId, string Date, string RuleName, string Hint);

/// <summary>
/// Final result broadcast when the GA loop finishes normally.
/// </summary>
/// <param name="JobId">Unique job identifier</param>
/// <param name="FinalHardViolations">Stage-0 value of the final best scenario</param>
/// <param name="FinalStage1Completion">Stage-1 completion rate (agents who reached GuaranteedHours)</param>
/// <param name="TokenCount">Number of assigned tokens in the final scenario</param>
/// <param name="AvailableShiftSlots">Total number of shift slots requested for the period (denominator for slot coverage)</param>
/// <param name="Tokens">Non-locked planned assignment tokens</param>
/// <param name="Awards">Auction awards per slot (Phase 3 telemetry, may be empty if auction did not seed the best scenario)</param>
/// <param name="Escalations">Stage-1 escalations recorded during the auction seed</param>
public sealed record WizardJobResultDto(
    Guid JobId,
    int FinalHardViolations,
    double FinalStage1Completion,
    int TokenCount,
    int AvailableShiftSlots,
    IReadOnlyList<WizardTokenDto> Tokens,
    IReadOnlyList<WizardAuctionAwardDto> Awards,
    IReadOnlyList<WizardEscalationDto> Escalations);
