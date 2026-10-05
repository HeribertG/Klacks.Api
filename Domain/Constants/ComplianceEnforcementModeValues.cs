// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The stored spellings of a compliance rule's reaction in the settings store (COMPLIANCE_ENFORCEMENT_*):
/// the enforcement resolver parses exactly these, and every writer normalises to them.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ComplianceEnforcementModeValues
{
    public const string Warn = "warn";
    public const string Block = "block";

    /// <summary>
    /// Maps any casing or surrounding whitespace of warn/block to the stored spelling; null for anything else.
    /// </summary>
    /// <param name="value">Raw mode text, e.g. from a skill parameter</param>
    public static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        if (string.Equals(trimmed, Warn, StringComparison.OrdinalIgnoreCase))
        {
            return Warn;
        }

        if (string.Equals(trimmed, Block, StringComparison.OrdinalIgnoreCase))
        {
            return Block;
        }

        return null;
    }
}
