// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Filter;

public class CalendarRulesFilter : BaseFilter
{
    public string Language { get; set; } = string.Empty;
    
    public List<StateCountryToken> List { get; set; } = new List<StateCountryToken>();
}
