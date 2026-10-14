// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one refusal text for a chat write on a Work whose lock level is above None: it names the exact level and the
/// way out for each level, so update_work, reassign_work and bulk_delete_works tell the user the same thing.
/// </summary>
/// <param name="workId">Id of the locked Work</param>
/// <param name="lockLevel">The Work's lock level</param>
/// <param name="action">What was asked for, as a past participle, e.g. "edited" or "handed over"</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class WorkLockRefusal
{
    public static string Message(Guid workId, WorkLockLevel lockLevel, string action) =>
        $"Work '{workId}' is locked ({lockLevel}) and cannot be {action}. " +
        "Confirmed works need unconfirm_work first, approved days revoke_day_approval, " +
        "and closed periods reopen_period (which returns an entry to its pre-close level, so a confirmed or " +
        "approved one still needs unconfirm_work or revoke_day_approval afterwards).";
}
