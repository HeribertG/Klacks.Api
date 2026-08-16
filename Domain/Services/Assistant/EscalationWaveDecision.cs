// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant;

/// <param name="IsParallel">True when every currently-pending stage should be notified in this round.</param>
/// <param name="StageCount">How many stages, counted from the lowest pending rank, this round notifies: 1 when serial, all of them when parallel.</param>
/// <param name="Duration">How long the notified stage(s) get before they are due for expiry.</param>
public readonly record struct EscalationWaveDecision(bool IsParallel, int StageCount, TimeSpan Duration);
