// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Notifications;

public record ShiftStatsNotificationDto
{
    public Guid ShiftId { get; init; }
    public DateTime Date { get; init; }
    public int Engaged { get; init; }
    public string SourceConnectionId { get; init; } = string.Empty;
    public Guid? AnalyseToken { get; init; }
}
