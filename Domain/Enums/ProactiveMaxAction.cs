// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How far Klacksy may go on its own for one trigger kind. The ladder is cumulative: Prepare implies
/// the reporting of Hint, Execute implies the scenario of Prepare. Hint is the fail-safe default, so a
/// kind nobody configured behaves exactly as the pipeline did before governance existed - it reports
/// and waits for a human. This governs the ACTION branch only; notification mute, snooze and rate
/// limits stay per-user preferences and are never consulted here.
/// </summary>
public enum ProactiveMaxAction
{
    /// <summary>Report the finding to the planners and stop. No scenario, no write.</summary>
    Hint = 0,

    /// <summary>Additionally lay a ready-made scenario in front of a human, who accepts or rejects it.</summary>
    Prepare = 1,

    /// <summary>Additionally carry the remediation out and file a mandatory report afterwards.</summary>
    Execute = 2
}
