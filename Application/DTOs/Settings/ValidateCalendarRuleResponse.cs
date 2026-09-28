// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Settings;

public class ValidateCalendarRuleResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public DateOnly? CalculatedDate { get; set; }
    public string? FormattedDate { get; set; }
    public string? DayOfWeek { get; set; }
    public int Year { get; set; }
}
