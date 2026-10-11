// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants of the accept_scenario skill's stale-scenario confirmation: the hidden parameter that carries the user's
/// explicit acknowledgement ("accept anyway, overwrite the newer changes") into the replayed skill call. It is never
/// declared in the skill definition and only honoured on a confirmed replay.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ScenarioAcceptDefaults
{
    public const string StaleConfirmedParameter = "staleScenarioOverwriteConfirmed";

    public const string ConfirmedValue = "true";
}
