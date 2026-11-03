// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// The one check a replacement WorkChange passes before it is written, for both create and update: a replacement
/// (ReplacementStart, ReplacementEnd, ReplacementWithin) names a person who is an existing, not deleted employee or
/// external employee, and the person does not end up with a blocking conflict in the time handed over.
/// </summary>
public interface IReplacementWriteGuard
{
    /// <summary>
    /// Throws InvalidRequestException (400) when a replacement names no valid person, and ConflictException when the
    /// substitute would get a blocking conflict. Group visibility of the clients is decided by the caller before.
    /// </summary>
    /// <param name="workChange">The change about to be written (type, times and optional ReplaceClientId)</param>
    /// <param name="parentWork">The Work the change is attached to; supplies date, shift and scenario token</param>
    /// <param name="overrideBlockRequested">Whether the caller asked to override a block-mode escalation</param>
    /// <param name="storedWorkChange">The change as stored before an update; null on create. An update of the same person and Work that stays within the stored window is not checked for conflicts; one that goes beyond it is checked with its whole window, and the stored window comes off the hours</param>
    Task EnsureAllowedAsync(
        WorkChange workChange,
        Work parentWork,
        bool overrideBlockRequested,
        WorkChange? storedWorkChange,
        CancellationToken cancellationToken);
}
