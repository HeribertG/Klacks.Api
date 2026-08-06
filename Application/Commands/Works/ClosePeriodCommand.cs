// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

/// <summary>
/// Seals the whole period.
/// </summary>
/// <param name="AcknowledgeViolations">
/// Confirms that the open violations of the period were seen and the seal is wanted anyway. Sealing
/// makes the days unwritable, so it must be a decision rather than a side effect.
/// </param>
public record ClosePeriodCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    bool AcknowledgeViolations = false) : IRequest<int>;
