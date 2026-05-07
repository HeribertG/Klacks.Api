// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

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
