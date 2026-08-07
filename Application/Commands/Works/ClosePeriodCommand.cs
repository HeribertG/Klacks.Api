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
/// <param name="AcknowledgedErrorCount">
/// Number of errors the confirmation was issued for. When set, the handler re-reads the findings and
/// refuses again if the period meanwhile holds more of them, so nothing that appeared between the
/// refusal and the confirmation is sealed over unseen. Null keeps the legacy behaviour of sealing on
/// the confirmation alone.
/// </param>
public record ClosePeriodCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    bool AcknowledgeViolations = false,
    int? AcknowledgedErrorCount = null) : IRequest<int>;
