// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// What seal_shift would seal right now, as ISealShiftTargetResolver works it out for both the preview and the confirmed
/// replay: the sealable order together with the group derived for it when it has none, or the refusal. Either Order or
/// Refusal is set; DerivedGroup only accompanies an Order.
/// </summary>
/// <param name="Order">The loaded order when it may be sealed; null on a refusal.</param>
/// <param name="DerivedGroup">The group the seal attaches because the order has none; null when the order has its own group.</param>
/// <param name="Refusal">Why the order cannot be sealed, with what the model should do next; null when sealable.</param>
public sealed record SealShiftTarget(Shift? Order, OrderGroupAssignment? DerivedGroup, string? Refusal);
