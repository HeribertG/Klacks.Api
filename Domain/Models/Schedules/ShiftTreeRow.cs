// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// One shift reduced to its place in the order tree: the sealed order, its plannable copy and the cut pieces.
/// </summary>
/// <param name="Id">Shift id</param>
/// <param name="Status">Life-cycle stage (order, sealed order, plannable copy, cut piece)</param>
/// <param name="OriginalId">Sealed order every plannable piece of the order points at</param>
/// <param name="ParentId">Piece this piece was cut from; null for a top-level piece</param>
/// <param name="RootId">Top-level piece of the cut tree</param>
public sealed record ShiftTreeRow(
    Guid Id,
    ShiftStatus Status,
    Guid? OriginalId,
    Guid? ParentId,
    Guid? RootId);