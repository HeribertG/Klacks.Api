// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses the skill-facing company-rule kind token (surchargeSettings, counterRule, customMacro) into the
/// <see cref="CompanyRuleKind"/> enum, returning a clear error listing the accepted tokens otherwise.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills.CompanyRules;

internal static class CompanyRuleKindParser
{
    private const string SurchargeSettings = "surchargeSettings";
    private const string CounterRule = "counterRule";
    private const string CustomMacro = "customMacro";

    public static bool TryParse(string? raw, out CompanyRuleKind kind, out string? error)
    {
        kind = default;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = $"kind is required — one of {SurchargeSettings}, {CounterRule}, {CustomMacro}.";
            return false;
        }

        var value = raw.Trim();
        if (string.Equals(value, SurchargeSettings, StringComparison.OrdinalIgnoreCase))
        {
            kind = CompanyRuleKind.SurchargeSettings;
            return true;
        }

        if (string.Equals(value, CounterRule, StringComparison.OrdinalIgnoreCase))
        {
            kind = CompanyRuleKind.CounterRule;
            return true;
        }

        if (string.Equals(value, CustomMacro, StringComparison.OrdinalIgnoreCase))
        {
            kind = CompanyRuleKind.CustomMacro;
            return true;
        }

        error = $"Unknown kind '{raw}'. Use one of {SurchargeSettings}, {CounterRule}, {CustomMacro}.";
        return false;
    }
}
