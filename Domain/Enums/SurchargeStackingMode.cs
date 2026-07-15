// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Controls how the overtime surcharge (K3) combines with the macro's own Night/Weekend/Holiday
/// surcharge selection (K4). HighestWins keeps today's behavior (the higher of the two totals wins,
/// nothing stacks) and is the default for every installation that never configures this — no
/// pre-existing customer's computed amounts change. Additive adds both on top of each other.
/// </summary>
public enum SurchargeStackingMode
{
    HighestWins = 0,
    Additive = 1
}
