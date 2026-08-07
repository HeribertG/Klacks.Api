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
/// <param name="AcknowledgedErrorCount">
/// Number of errors the confirmation was issued for. When set, the handler re-reads the findings and
/// refuses again if the period meanwhile holds more of them, so nothing that appeared between the
/// refusal and the confirmation is sealed over unseen. Null keeps the legacy behaviour of sealing on
/// the confirmation alone.
/// </param>
public record ClosePeriodByGroupCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? GroupId,
    string? Reason,
    bool AcknowledgeViolations = false,
    int? AcknowledgedErrorCount = null
) : IRequest<int>;
