// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The identity of one validation finding: comment key, date, client and the comment parameters. The parameters
/// belong to it, so two findings of the same kind on the same day stay two, a second qualification below its level
/// or a second colliding block. Aggregate findings carry their bucket total as a parameter, so a changed total is a
/// different finding.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Services.Schedules;

public static class ConflictFingerprint
{
    private const string ParameterSeparator = ",";

    public static string Of(ScheduleValidationNotificationDto entry)
    {
        var paramSignature = string.Join(
            ParameterSeparator,
            entry.CommentParams.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
        return $"{entry.Comment}|{entry.Date:O}|{entry.ClientId}|{paramSignature}";
    }
}
