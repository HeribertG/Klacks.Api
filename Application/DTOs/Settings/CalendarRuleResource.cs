// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.Settings;

public class CalendarRuleResource
{
    public Guid? Id { get; set; }

    public string Country { get; set; } = string.Empty;

    public MultiLanguage? Description { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsPaid { get; set; }

    public MultiLanguage? Name { get; set; }

    public string Rule { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string SubRule { get; set; } = string.Empty;
}
