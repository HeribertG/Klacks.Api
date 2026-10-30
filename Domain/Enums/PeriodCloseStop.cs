// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The first shared gate that stops a group's period from being closed, in the order PeriodCloseGates checks them.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum PeriodCloseStop
{
    /// <summary>No shared gate stops the period.</summary>
    None = 0,

    /// <summary>The last day of the period is sealed (group or installation-wide lock): the period counts as closed.</summary>
    LastDaySealed = 1,

    /// <summary>No work on the group's OWN shifts falls into the period, so a group seal would act on nothing.</summary>
    NoDirectWork = 2,

    /// <summary>The period holds an unseal entry: somebody reopened it on purpose.</summary>
    ReopenedBefore = 3
}
