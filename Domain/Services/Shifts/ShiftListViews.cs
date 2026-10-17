// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Classifies shift list requests. The unsealed-draft review queue (order intake) ignores the date range and the
/// UI group selection; group visibility still applies to it.
/// </summary>
/// <param name="filter">Shift list filter sent by the client</param>

using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftListViews
{
    public static bool IsUnsealedDraftView(ShiftFilter filter)
        => filter.FilterType == ShiftFilterType.Original && filter.IsSealedOrder == false;
}
