// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Settings;

public class CompanyRuleResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RuleText { get; set; } = string.Empty;

    public CompanyRuleKind Kind { get; set; }

    public string TargetEntityType { get; set; } = string.Empty;

    public Guid? TargetEntityId { get; set; }

    public DateTime AppliedUtc { get; set; }
}
