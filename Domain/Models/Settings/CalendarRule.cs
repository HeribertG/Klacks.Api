// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Settings;

public class CalendarRule
{
    public string Country { get; set; } = string.Empty;

    public MultiLanguage Description { get; set; } = new();

    [Key]
    public Guid Id { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsPaid { get; set; } = CalendarRuleDefaults.IsPaid;

    public MultiLanguage Name { get; set; } = new();

    public string Rule { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string SubRule { get; set; } = string.Empty;
}
