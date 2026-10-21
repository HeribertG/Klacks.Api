// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the scheduling rule an industry-bound import row (cap, rotation, counter rule, rate
/// revision) belongs to.
/// </summary>
/// <param name="entitySourceKey">ImportSourceKey of the bound row</param>
/// <param name="ruleSourceKeyByBoundEntityKey">Bound row key to its block rule's source key</param>
/// <param name="ruleIdBySourceKey">Rule source key to the id the rule has after this import</param>

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class ImportBoundRuleResolver
{
    // The binding (industry cap/rotation -> block rule) is part of the row's identity, not of its
    // editable values: it is re-applied on every import (also on Update) and never hashed - a stale
    // binding after a file edit is an import concern, not a customer edit.
    internal static Guid? ResolveBoundRuleId(
        string entitySourceKey,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        return ruleSourceKeyByBoundEntityKey.TryGetValue(entitySourceKey, out var ruleKey)
               && ruleIdBySourceKey.TryGetValue(ruleKey, out var ruleId)
            ? ruleId
            : null;
    }
}
