// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Notifications;

public record PeriodHoursRecalculatedDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public Guid? AnalyseToken { get; init; }
}
