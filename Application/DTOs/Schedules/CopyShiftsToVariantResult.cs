// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of copying existing shifts into a variant (key).
/// </summary>
/// <param name="CopiedFamilies">Number of order families copied, each with the rows of its sealed order, plannable copy and cut pieces that overlap the variant's period</param>
/// <param name="SkippedFamilies">Number of families left out when a whole group was copied: open orders, families without a customer, outside the variant's period or with a group outside the variant's hierarchy, and families that cannot be cut down to the period</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public record CopyShiftsToVariantResult(int CopiedFamilies, int SkippedFamilies);
