// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant;

/// <param name="MinStageMinutes">Floor for one stage's turn (ESCALATION_STAGE_MIN_MINUTES).</param>
/// <param name="MaxStageMinutes">Ceiling for one stage's turn (ESCALATION_STAGE_MAX_MINUTES).</param>
/// <param name="PrepBufferHours">Fixed buffer subtracted from shift start to get the chain deadline (ESCALATION_PREP_BUFFER_HOURS).</param>
public readonly record struct EscalationTimeBudget(int MinStageMinutes, int MaxStageMinutes, int PrepBufferHours);
