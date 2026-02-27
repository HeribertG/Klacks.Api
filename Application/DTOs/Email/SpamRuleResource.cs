// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Email;

public class SpamRuleResource
{
    public Guid Id { get; set; }

    public SpamRuleType RuleType { get; set; }

    public string Pattern { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }
}
