// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared precondition of the accept and reject handlers: a scenario can only be decided once.
/// Accepting an already decided scenario again would soft-delete the real schedule data of its period
/// with nothing left under the token to promote; rejecting it later would rewrite its status and the
/// capture/ledger outcome. Both are refused with a coded 409 before any data is touched.
/// </summary>
/// <param name="scenario">The scenario about to be accepted or rejected</param>

using System.Globalization;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

internal static class AnalyseScenarioStatusGuard
{
    private const string NotActiveMessageFormat =
        "AnalyseScenario {0} is no longer active (status {1}); it has already been decided and cannot be accepted or rejected again.";

    public static void EnsureActive(AnalyseScenario scenario)
    {
        if (scenario.Status != AnalyseScenarioStatus.Active)
        {
            throw new ScenarioNotActiveException(string.Format(CultureInfo.InvariantCulture, NotActiveMessageFormat, scenario.Id, scenario.Status));
        }
    }
}
