// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// The group IOrderGroupPlacementResolver derived for one open order without a group: either the planned assignment (the
/// group and why it was chosen) or the reason no group the caller may see could be derived. Exactly one of the two is set.
/// </summary>
/// <param name="Assignment">The planned order-to-group placement; null when no group was derived.</param>
/// <param name="UnplacedReason">Why no group was derived; null when one was.</param>
public sealed record OrderGroupPlacement(OrderGroupAssignment? Assignment, string? UnplacedReason);
