// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// What became of an order in the order review: still to do, changed, sealed and cut, failed (with its reason) or left
/// out by the user.
/// </summary>
public enum OrderReviewExecutionStatus
{
    Pending = 0,
    Executed = 1,
    Failed = 2,
    Skipped = 3,
}
