// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services;

public class TimelineCheckRequest
{
    public Guid ClientId { get; init; }
    public DateOnly Date { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsRangeCheck { get; init; }
    public Guid? AnalyseToken { get; init; }
}
