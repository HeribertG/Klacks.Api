// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

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
/// <param name="TimedOut">True when the GA stopped because the soft time budget elapsed; the result is the best solution found up to that point</param>
public sealed record WizardJobResultDto(
    Guid JobId,
    int FinalHardViolations,
    double FinalStage1Completion,
    int TokenCount,
    int AvailableShiftSlots,
    IReadOnlyList<WizardTokenDto> Tokens,
    IReadOnlyList<WizardAuctionAwardDto> Awards,
    IReadOnlyList<WizardEscalationDto> Escalations,
    bool TimedOut = false);
