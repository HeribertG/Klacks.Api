// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.DTOs.Filter;

public class TruncatedCalendarRule : BaseTruncatedResult
{
    public ICollection<CalendarRule> CalendarRules { get; set; } = null!;
}
