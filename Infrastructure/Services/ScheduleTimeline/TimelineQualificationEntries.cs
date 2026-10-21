// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Notification-only qualification check for the live editing path: for every placed work, report
/// any required-qualification gap of the assigned agent as a schedule-validation entry. A completely
/// missing mandatory qualification and one held below a binding level are Errors; a non-binding too-low
/// level, expired or optional gaps are Warnings (QualificationGap.Severity).
/// Never blocks the save — it mirrors how collisions/overtime are surfaced post-commit.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public static class TimelineQualificationEntries
{
    public static async Task AddAsync(
        List<ScheduleValidationNotificationDto> entries,
        IReadOnlyList<Work> works,
        IReadOnlyDictionary<Guid, string> clientNameLookup,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder,
        CancellationToken cancellationToken)
    {
        var relevant = works.Where(w => w.ShiftId != Guid.Empty).ToList();
        if (relevant.Count == 0)
        {
            return;
        }

        var agentIds = relevant.Select(w => w.ClientId).Distinct().ToList();
        var slots = relevant
            .Select(w => new EligibilitySlot(w.ShiftId, w.CurrentDate))
            .Distinct()
            .ToList();

        var matrix = await eligibilityMatrixBuilder.BuildAsync(agentIds, slots, ct: cancellationToken);
        if (matrix.Gaps.Count == 0)
        {
            return;
        }

        foreach (var work in relevant)
        {
            var key = (work.ClientId.ToString(), work.ShiftId, work.CurrentDate);
            if (!matrix.Gaps.TryGetValue(key, out var gaps))
            {
                continue;
            }

            clientNameLookup.TryGetValue(work.ClientId, out var clientName);
            foreach (var gap in gaps)
            {
                entries.Add(ToEntry(work, clientName ?? string.Empty, gap));
            }
        }
    }

    private static ScheduleValidationNotificationDto ToEntry(Work work, string clientName, QualificationGap gap)
        => new()
        {
            Type = gap.Severity == QualificationGapSeverity.Error
                ? ScheduleValidationType.Error
                : ScheduleValidationType.Warning,
            ClientId = work.ClientId,
            ClientName = clientName,
            Date = work.CurrentDate,
            Comment = QualificationValidationKeys.ForReason(gap.Reason),
            CommentParams = QualificationGapCommentParams.Build(gap),
        };
}
