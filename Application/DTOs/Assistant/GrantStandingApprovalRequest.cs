// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request body of a new standing approval. The duration is asked for in DAYS rather than as an
/// expiry instant, so the value cannot mean two different things depending on which time zone the
/// administrator's browser reported; the server turns it into an instant from its own clock. Both
/// optional fields fall back to the StandingApprovalDefaults values.
/// </summary>

namespace Klacks.Api.Application.DTOs.Assistant;

public record GrantStandingApprovalRequest(
    string TriggerKind,
    Guid? GroupId,
    int? DurationDays,
    int? DailyBudget);
