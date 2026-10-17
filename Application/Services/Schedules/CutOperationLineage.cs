// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// The server-owned fields one cut operation writes, resolved before anything is staged: for an update the values as
/// stored, for a new piece the order reference and customer of its parent. The request never sets them.
/// </summary>
/// <param name="OriginalId">Sealed order the piece belongs to</param>
/// <param name="ClientId">Customer of the order</param>
/// <param name="ParentId">Stored parent of an updated piece; ignored for a new piece</param>
/// <param name="RootId">Stored tree root of an updated piece; ignored for a new piece</param>
/// <param name="Lft">Stored nested-set left value of an updated piece</param>
/// <param name="Rgt">Stored nested-set right value of an updated piece</param>
/// <param name="StoredStatus">Stored status of an updated shift; null for a new piece</param>
internal sealed record CutOperationLineage(
    Guid? OriginalId,
    Guid? ClientId,
    Guid? ParentId,
    Guid? RootId,
    int? Lft,
    int? Rgt,
    ShiftStatus? StoredStatus);
