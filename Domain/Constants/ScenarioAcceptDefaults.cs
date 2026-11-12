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
/// The two refusals no confirmation lifts are reported by the skill without a confirmation token:
/// CoverNotOverridableMessageFormat (scenario id, changed entry count, accepted scenarios text, prefill text built with
/// CoverRedoPresetFormat or empty) for a stale scenario whose source chain starts at a cover built on the original plan, DropsRealAbsenceMessageFormat (scenario
/// id, count, list of name and day) for an accept that would delete a real absence the scenario does not carry.
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

    public const string CoverNotOverridableMessageFormat =
        "Scenario {0} was NOT accepted and cannot be accepted: it is the cover of an absence built on the original plan, or a scenario built on such a cover, and the original plan changed after that cover was created ({1} changed entries in its scope{2}). Such a cover is never accepted over newer changes, and no confirmation lifts this. Tell the user to reject this cover and cover the absence again on the current plan{3}. When several absences are covered one after the other on the original plan, every cover after the first accepted one has to be covered again.";

    public const string CoverRedoPresetFormat = " (person {0}, {1} to {2})";

    public const string AcceptedSinceScenariosFormat = "; scenarios accepted since then: {0}";

    public const string AbsenceEntryFormat = "{0} {1}";

    public const string DropsRealAbsenceMessageFormat =
        "Scenario {0} was NOT accepted and cannot be accepted: accepting it would delete {1} real absence(s) it does not contain ({2}). No override and no confirmation lifts this; absences are never deleted by an accept. Tell the user which absences are missing and that the scenario has to be recalculated on the current plan (for example run the planning again, or cover the absence again).";

    public static readonly IReadOnlyList<string> ReservedParameters =
    [
        AcknowledgedChangedEntryCountParameter,
        AcknowledgedLastChangeUtcTicksParameter,
        LegacyStaleConfirmedParameter
    ];
}
