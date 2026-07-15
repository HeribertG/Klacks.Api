// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// How a violated compliance rule (e.g. MaxDailyHours, MinRestHours) is surfaced to the caller:
/// Warn keeps today's behavior (reported, never blocks a save); Block escalates a newly introduced
/// violation to a hard error that a save-command handler refuses to commit.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum RuleEnforcementMode
{
    Warn = 0,
    Block = 1,
}
