// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to reject an AnalyseScenario, optionally with a structured reason for the learning flywheel.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to reject</param>
/// <param name="Reason">Structured rejection reason (optional)</param>
/// <param name="ReasonText">Optional free-text rejection note</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record RejectAnalyseScenarioCommand(Guid ScenarioId, RejectReason? Reason = null, string? ReasonText = null) : IRequest<bool>;
