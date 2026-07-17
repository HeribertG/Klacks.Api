// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runtime tracking record for a statutory compensatory-rest deadline (K12 stage 1). When a rest gap
/// between two consecutive work blocks falls below the client's minimum rest, an obligation is created
/// so the shortfall must be compensated by an extended later rest before <see cref="DueDate"/>.
/// Rows are written exclusively by <c>ICompensatoryRestObligationReconciler</c>; the natural key is
/// (<see cref="ClientId"/>, <see cref="RestGapStart"/>). <see cref="StandardRestHours"/> is a snapshot
/// of the policy minimum at trigger time: the fulfilment threshold is composed only of snapshot-anchored
/// values (<see cref="StandardRestHours"/> + <see cref="ShortfallHours"/>) so the compensation
/// requirement stays stable if the policy changes later. Not an imported rule but derived state, so no
/// IImportableEntity.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Scheduling;

public class CompensatoryRestObligation : BaseEntity
{
    public Guid ClientId { get; set; }

    public DateOnly TriggerDate { get; set; }

    public DateTime RestGapStart { get; set; }

    public decimal ShortfallHours { get; set; }

    public decimal StandardRestHours { get; set; }

    public DateOnly DueDate { get; set; }

    public DateOnly? FulfilledOn { get; set; }
}
