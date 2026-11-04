// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What an absence cover must know about one scenario work to decide between moving it and a replacement.
/// </summary>
/// <param name="WorkId">The scenario work</param>
/// <param name="ClientId">Its current owner</param>
/// <param name="ParentWorkId">Its container, null for a top-level work</param>
/// <param name="CurrentDate">Workday</param>
/// <param name="StartTime">Start of the work</param>
/// <param name="EndTime">End of the work</param>
/// <param name="LockLevel">Seal state of the work</param>
/// <param name="HasReplacement">A live replacement sits on the work or on one of its sub-works</param>
/// <param name="HasLockedChild">A sub-work or sub-break of the work is sealed</param>
/// <param name="IsMoved">An earlier cover moved the work to its owner</param>
/// <param name="IsContainer">The work is a container; it changes person only as a whole</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record CoverMoveFacts(
    Guid WorkId,
    Guid ClientId,
    Guid? ParentWorkId,
    DateOnly CurrentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    WorkLockLevel LockLevel,
    bool HasReplacement,
    bool HasLockedChild,
    bool IsMoved = false,
    bool IsContainer = false);
