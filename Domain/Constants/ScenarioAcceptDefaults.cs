// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants of the accept_scenario skill's stale-scenario confirmation. The staleness the user was shown is stored
/// with the pending confirmation (purpose PendingConfirmationPurposes.StaleScenarioOverwrite) under the two
/// Acknowledged* keys; confirm_pending_action moves it into SkillExecutionContext and removes the keys before the
/// replay. ReservedParameters lists every key that may carry this consent, including the former hidden flag: the skill
/// executor drops them from every incoming invocation, so a model or a background caller can never supply them.
/// It also holds the refusal texts of the variant-key accept rule: VariantAcceptForbiddenMessage when the caller lacks the
/// right to edit shifts or has no session, VariantConsumedMessageFormat (one argument: the consumed key names) when a key
/// the scenario carries is no longer Active.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ScenarioAcceptDefaults
{
    public const string SkillName = "accept_scenario";

    public const string AcknowledgedChangedEntryCountParameter = "staleAcknowledgedChangedEntryCount";

    public const string AcknowledgedLastChangeUtcTicksParameter = "staleAcknowledgedLastChangeUtcTicks";

    public const string LegacyStaleConfirmedParameter = "staleScenarioOverwriteConfirmed";

    public const string VariantAcceptForbiddenMessage =
        "Accepting a scenario that carries variant keys needs the right to edit shifts in a signed-in session. Callers without a session (Slack bridge, plan steps, scheduled tasks, automatic period commit) cannot accept it.";

    public const string VariantConsumedMessageFormat =
        "The scenario carries variant keys that have already been consumed and can no longer be accepted with it: {0}";

    public static readonly IReadOnlyList<string> ReservedParameters =
    [
        AcknowledgedChangedEntryCountParameter,
        AcknowledgedLastChangeUtcTicksParameter,
        LegacyStaleConfirmedParameter
    ];
}
