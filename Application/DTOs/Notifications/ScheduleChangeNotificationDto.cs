// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Notifications;

public record ScheduleChangeNotificationDto
{
    public Guid ClientId { get; init; }
    public DateOnly ChangeDate { get; init; }
    public string SourceConnectionId { get; init; } = string.Empty;
    public Guid? AnalyseToken { get; init; }
}
