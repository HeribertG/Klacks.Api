// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A company rule the assistant is collecting from the admin across dialog turns: the target rule
/// <see cref="Kind"/>, the admin's original wording (<see cref="RuleText"/>) and the string-valued
/// parameters gathered so far. Held in the pending draft store keyed by user and conversation; each
/// successful Set (start or parameter update) slides <see cref="ExpiresAtUtc"/> forward by the store TTL.
/// </summary>

using System;
using System.Collections.Generic;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class CompanyRuleDraft
{
    public CompanyRuleKind Kind { get; set; }

    public string RuleText { get; set; } = string.Empty;

    public string? Name { get; set; }

    public Dictionary<string, string> Parameters { get; set; } = new();

    public DateTime ExpiresAtUtc { get; set; }
}
