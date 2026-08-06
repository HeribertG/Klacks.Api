// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PeriodClosing;

/// <summary>
/// Seals a period, optionally scoped to a group.
/// </summary>
/// <param name="AcknowledgeViolations">
/// Confirms that the open violations of the period were seen and the seal is wanted anyway. Closing
/// is never refused outright, but it must not happen silently either: without this the handler
/// reports how many errors the period still holds and seals nothing.
/// </param>
public record ClosePeriodByGroupCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? GroupId,
    string? Reason,
    bool AcknowledgeViolations = false
) : IRequest<int>;
