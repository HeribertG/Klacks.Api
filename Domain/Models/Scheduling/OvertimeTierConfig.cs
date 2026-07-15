// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

/// <summary>
/// One active overtime tier: the cumulative hours in the configured OvertimeBasis period after which
/// this tier's rate applies, up to the next tier's AfterHours (or unbounded for the last configured
/// tier), and the typed surcharge (Overtime1/2/3) that band is reported under.
/// </summary>
/// <param name="AfterHours">Cumulative hours in the period after which this tier's rate applies</param>
/// <param name="Rate">The rate multiplier or fixed-per-hour amount for hours in this tier's band</param>
/// <param name="Type">The typed surcharge (Overtime1/2/3) this tier's band is reported under</param>
public sealed record OvertimeTierConfig(decimal AfterHours, decimal Rate, SurchargeType Type);
