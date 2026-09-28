// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class ScheduleCommandResource
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public DateOnly CurrentDate { get; set; }
    public string CommandKeyword { get; set; } = string.Empty;
    public Guid? AnalyseToken { get; set; }
}
