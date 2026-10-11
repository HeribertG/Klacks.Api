// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants of the accept_scenario skill's stale-scenario confirmation. The staleness the user was shown is stored
/// with the pending confirmation (purpose PendingConfirmationPurposes.StaleScenarioOverwrite) under the two
/// Acknowledged* keys; confirm_pending_action moves it into SkillExecutionContext and removes the keys before the
/// replay. ReservedParameters lists every key that may carry this consent, including the former hidden flag: the skill
/// executor drops them from every incoming invocation, so a model or a background caller can never supply them.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ScenarioAcceptDefaults
{
    public const string SkillName = "accept_scenario";

    public const string AcknowledgedChangedEntryCountParameter = "staleAcknowledgedChangedEntryCount";

    public const string AcknowledgedLastChangeUtcTicksParameter = "staleAcknowledgedLastChangeUtcTicks";

    public const string LegacyStaleConfirmedParameter = "staleScenarioOverwriteConfirmed";

    public static readonly IReadOnlyList<string> ReservedParameters =
    [
        AcknowledgedChangedEntryCountParameter,
        AcknowledgedLastChangeUtcTicksParameter,
        LegacyStaleConfirmedParameter
    ];
}
