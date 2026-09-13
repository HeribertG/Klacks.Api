// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record AcceptAnalyseScenarioCommand(
    Guid ScenarioId,
    bool OverrideBlock = false,
    Guid? ActingUserId = null) : IRequest<bool>;
