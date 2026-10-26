// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// Read-only verdict of IOrderSealingService.CheckAsync on one order: either the loaded order, which passes every
/// sealing requirement right now, or the refusal SealAsync would return for it. Exactly one of the two is set.
/// </summary>
/// <param name="SealableOrder">The loaded order when it may be sealed; null on a refusal.</param>
/// <param name="Refusal">The refusal outcome (not found or hidden, not an order, already sealed, missing fields); null when sealable.</param>
public sealed record OrderSealingCheck(Shift? SealableOrder, OrderSealingOutcome? Refusal);
