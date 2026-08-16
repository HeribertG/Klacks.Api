// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <param name="WorkId">The shift the outage affects.</param>
/// <param name="GroupId">Group the shift belongs to; resolved to its Nested Set root for the roster lookup.</param>
/// <param name="AbsentClientId">The employee who fell out.</param>
/// <param name="AbsentClientName">Snapshot taken at chain start; not re-resolved by a join afterwards.</param>
/// <param name="ShiftStartUtc">Frozen into EscalationChain.ShiftStartUtc; a later shift edit must not move it.</param>
/// <param name="AbsenceBreakId">The Break row BulkAddBreaksCommandHandler recorded for this absence, when known; enables the F3 Superseded path.</param>
public readonly record struct StartEscalationChainRequest(
    Guid WorkId,
    Guid GroupId,
    Guid AbsentClientId,
    string AbsentClientName,
    DateTime ShiftStartUtc,
    Guid? AbsenceBreakId);
