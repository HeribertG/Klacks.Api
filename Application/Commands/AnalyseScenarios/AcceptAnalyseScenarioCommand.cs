// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Command to accept an AnalyseScenario.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to accept</param>
/// <param name="OverrideBlock">Requests the K1 supervisor override for Block-mode compliance violations</param>
/// <param name="ActingUserId">
/// The user the acceptance is recorded under when there is no HTTP request to read a claim from - the
/// autonomous next-period commit passes the admin whose autonomy preference released the run. A request
/// can never override its own identity with it: the handler prefers the NameIdentifier claim whenever
/// one exists. Null (the default) keeps the request-driven behaviour unchanged.
/// </param>
/// <param name="OverrideStale">
/// The caller acknowledged that the original plan changed after the scenario was created and that accepting
/// overwrites those changes. Independent of OverrideBlock: neither flag ever passes the other check.
/// </param>
/// <param name="AcknowledgedStaleness">
/// With OverrideStale: the staleness the user was shown when they confirmed. When the original plan changed further
/// since (more changed entries or a newer last change) the override no longer covers it and the accept is refused with
/// SCENARIO_STALE again. Null (REST override, autonomous commit) keeps the plain override.
/// </param>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record AcceptAnalyseScenarioCommand(
    Guid ScenarioId,
    bool OverrideBlock = false,
    Guid? ActingUserId = null,
    bool OverrideStale = false,
    StaleScenarioOverwriteAcknowledgement? AcknowledgedStaleness = null) : IRequest<bool>;
